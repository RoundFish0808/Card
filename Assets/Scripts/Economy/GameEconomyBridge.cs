using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

[DefaultExecutionOrder(-50)]
[DisallowMultipleComponent]
public class GameEconomyBridge : MonoBehaviour
{
    [Header("References")]
    public CardManager cardManager;

    [Header("Panel Names")]
    public string bigSuccessPanelName = "bigEndPanel_suc";
    public string bigFailPanelName = "bigEndPanel_fail";
    public bool autoShowBigPanel = true;

    [Header("Round Rules")]
    public int roundsPerSmallPanel = 1;
    public int roundsPerBigGame = 8;

    [Header("Economy Config")]
    public int startingPrincipalAmount = 5000;
    public int loanAmount = 1500000;
    public float firstLevelInterestRate = 0.02f;
    public float secondLevelInterestRate = 0.06f;
    public float thirdLevelInterestRate = 0.08f;
    public float cheatInterestAdd = 0.01f;
    public int firstLevelBetLimit = 5000;
    public int secondLevelBetLimit = 10000;
    public int thirdLevelBetLimit = 15000;

    private readonly List<EconomyRoundResultData> allRoundResults = new List<EconomyRoundResultData>();
    private readonly List<EconomyRoundResultData> pendingSmallPanelRounds = new List<EconomyRoundResultData>();
    private readonly List<SmallPanelResultData> smallPanelResults = new List<SmallPanelResultData>();

    private int previousPlayerTotalScore;
    private int previousNpcBattleTotalScore;
    private int cheatedRoundCount;
    private int currentAvailableAmount;
    private int nextGameInitialAvailableAmount = -1;

    public static GameEconomyBridge Instance { get; private set; }

    public bool IsSmallGameEnded { get; private set; }
    public bool IsBigGameEnded { get; private set; }
    public bool IsPlayerWin { get; private set; }
    public bool ShouldPauseForSmallPanel { get; private set; }

    public int CurrentRoundIndex { get; private set; }
    public int CurrentSmallIndex => smallPanelResults.Count + 1;
    public int TargetScore => CalculateInterestAmount(CurrentInterestRate);
    public int LoanAmount => loanAmount;
    public int CurrentBetLimit => GetCurrentBetLimit();
    public int CurrentBetAmount => CalculateBetAmount(currentAvailableAmount);
    public int CurrentAvailableAmount => currentAvailableAmount;
    public int PlayerTotalScore { get; private set; }
    public int NpcEconomicScore { get; private set; }
    public int NpcTotalScore => NpcEconomicScore;
    public int CheatedRoundCount => cheatedRoundCount;

    public float BaseInterestRate => GetCurrentBaseInterestRate();
    public float CheatInterestRate => cheatInterestAdd * cheatedRoundCount;
    public float CurrentInterestRate { get; private set; }

    public SmallPanelResultData LatestSmallPanelResult { get; private set; }
    public BigPanelResultData LatestBigPanelResult { get; private set; }
    public IReadOnlyList<EconomyRoundResultData> AllRoundResults => allRoundResults.AsReadOnly();
    public IReadOnlyList<SmallPanelResultData> SmallPanelResults => smallPanelResults.AsReadOnly();

    public event Action OnEconomyChanged;
    public event Action<SmallPanelResultData> OnSmallPanelResultReady;
    public event Action<BigPanelResultData> OnBigPanelResultReady;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureBridgeForLoadedScene()
    {
        CardManager manager = UnityEngine.Object.FindObjectOfType<CardManager>();
        if (manager == null || UnityEngine.Object.FindObjectOfType<GameEconomyBridge>() != null)
            return;

        GameEconomyBridge bridge = manager.gameObject.AddComponent<GameEconomyBridge>();
        bridge.cardManager = manager;
    }

    private void Awake()
    {
        Instance = this;
        currentAvailableAmount = startingPrincipalAmount;
        CurrentInterestRate = CalculateCurrentInterestRate();
        NpcEconomicScore = CalculateInterestAmount(CurrentInterestRate);
        if (cardManager != null)
            PlayerTotalScore = cardManager.PlayerTotalScore;
    }

    private void OnEnable()
    {
        ResolveReferences();
        Subscribe();
    }

    private void OnDisable()
    {
        Unsubscribe();
        if (Instance == this)
            Instance = null;
    }

    public void ResetEconomyState()
    {
        ResetEconomyState(false);
    }

    public void ResetEconomyState(bool useNextGameSettlement)
    {
        IsSmallGameEnded = false;
        IsBigGameEnded = false;
        IsPlayerWin = false;
        ShouldPauseForSmallPanel = false;
        CurrentRoundIndex = 0;
        PlayerTotalScore = 0;
        previousPlayerTotalScore = PlayerTotalScore;
        previousNpcBattleTotalScore = 0;
        currentAvailableAmount = useNextGameSettlement && nextGameInitialAvailableAmount >= 0 ? nextGameInitialAvailableAmount : startingPrincipalAmount;
        nextGameInitialAvailableAmount = -1;
        CurrentInterestRate = CalculateCurrentInterestRate();
        NpcEconomicScore = CalculateInterestAmount(CurrentInterestRate);
        LatestSmallPanelResult = null;
        LatestBigPanelResult = null;
        allRoundResults.Clear();
        pendingSmallPanelRounds.Clear();
        smallPanelResults.Clear();
        OnEconomyChanged?.Invoke();
    }

    public void PrepareNextGameFromLatestSettlement()
    {
        nextGameInitialAvailableAmount = LatestBigPanelResult != null ? Mathf.Max(0, LatestBigPanelResult.FinalSettlementAmount) : startingPrincipalAmount;
    }

    private void ResolveReferences()
    {
        if (cardManager == null)
            cardManager = FindObjectOfType<CardManager>();

        if (cardManager != null && allRoundResults.Count == 0)
        {
            PlayerTotalScore = cardManager.PlayerTotalScore;
            previousPlayerTotalScore = PlayerTotalScore;
        }
    }

    private void Subscribe()
    {
        if (cardManager == null)
            return;

        cardManager.OnTotalScoreChanged -= HandleTotalScoreChanged;
        cardManager.OnTotalScoreChanged += HandleTotalScoreChanged;
        cardManager.OnGameEnded -= HandleGameEnded;
        cardManager.OnGameEnded += HandleGameEnded;
    }

    private void Unsubscribe()
    {
        if (cardManager == null)
            return;

        cardManager.OnTotalScoreChanged -= HandleTotalScoreChanged;
        cardManager.OnGameEnded -= HandleGameEnded;
    }

    private void HandleTotalScoreChanged(int playerTotal, int npcBattleTotal)
    {
        if (IsBigGameEnded)
            return;

        IsSmallGameEnded = false;
        ShouldPauseForSmallPanel = false;

        CurrentRoundIndex = allRoundResults.Count + 1;
        int availableBeforeRound = allRoundResults.Count == 0 ? startingPrincipalAmount : currentAvailableAmount;
        if (allRoundResults.Count == 0)
            availableBeforeRound = currentAvailableAmount;

        int betLimit = CurrentBetLimit;
        int betAmount = CalculateBetAmount(availableBeforeRound);

        int fallbackFormulaBase = cardManager != null ? cardManager.LastSmallRoundFormulaBase : 0;
        int winDiffScore = cardManager != null ? cardManager.LastSmallRoundWinDiffScore : 0;
        int formulaBase = fallbackFormulaBase > 0 ? fallbackFormulaBase : winDiffScore + 6;
        string handTypeName = "";
        float handMultiplier = 1f;
        if (cardManager != null)
            handMultiplier = ScoreMultiplierRules.GetMultiplier(cardManager.CurrentSmallRoundPlayerCards, out handTypeName);

        int playerRoundScore = Mathf.RoundToInt(formulaBase * handMultiplier * betAmount);
        PlayerTotalScore = playerRoundScore;
        int npcBattleRoundScore = npcBattleTotal - previousNpcBattleTotalScore;
        previousPlayerTotalScore = playerRoundScore;
        previousNpcBattleTotalScore = npcBattleTotal;

        bool isCheated = cardManager != null && cardManager.CurrentSmallRoundUsedCheat;
        if (isCheated)
            cheatedRoundCount++;

        CurrentInterestRate = CalculateCurrentInterestRate();
        NpcEconomicScore = CalculateInterestAmount(CurrentInterestRate);
        int settlementAmount = playerRoundScore - NpcEconomicScore;
        currentAvailableAmount = settlementAmount;

        EconomyRoundResultData economyRound = new EconomyRoundResultData(
            CurrentRoundIndex,
            playerRoundScore,
            PlayerTotalScore,
            betAmount,
            availableBeforeRound,
            betLimit,
            winDiffScore,
            formulaBase,
            npcBattleRoundScore,
            npcBattleTotal,
            handTypeName,
            handMultiplier,
            isCheated,
            CurrentInterestRate,
            NpcEconomicScore,
            settlementAmount);

        allRoundResults.Add(economyRound);
        pendingSmallPanelRounds.Add(economyRound);

        if (settlementAmount < 0)
        {
            CompleteBigPanelResult("RoundSettlementNegative");
        }
        else if (ShouldCompleteBigGame())
        {
            CompleteBigPanelResult("RoundLimitReached");
        }
        else if (ShouldCompleteSmallPanel())
        {
            CompleteSmallPanelResult();
        }

        OnEconomyChanged?.Invoke();
    }

    private void HandleGameEnded(int playerTotal, int npcTotal, BattleResult result)
    {
        if (IsBigGameEnded)
            return;

        PlayerTotalScore = playerTotal;
        CompleteBigPanelResult("CardManagerGameEnded");
        OnEconomyChanged?.Invoke();
    }

    private bool ShouldCompleteSmallPanel()
    {
        if (roundsPerSmallPanel <= 0 || IsBigGameEnded)
            return false;

        return pendingSmallPanelRounds.Count >= roundsPerSmallPanel;
    }

    private bool ShouldCompleteBigGame()
    {
        return roundsPerBigGame > 0 && CurrentRoundIndex >= roundsPerBigGame;
    }

    private void CompleteSmallPanelResult()
    {
        IsSmallGameEnded = true;
        ShouldPauseForSmallPanel = true;

        List<EconomyRoundResultData> resultRounds = new List<EconomyRoundResultData>(pendingSmallPanelRounds);
        if (resultRounds.Count == 0)
        {
            IsSmallGameEnded = false;
            ShouldPauseForSmallPanel = false;
            return;
        }

        int playerSmallScore = resultRounds[resultRounds.Count - 1].PlayerScore;

        LatestSmallPanelResult = new SmallPanelResultData(
            smallPanelResults.Count + 1,
            resultRounds[resultRounds.Count - 1].RoundIndex,
            resultRounds[resultRounds.Count - 1].RoundIndex,
            playerSmallScore,
            resultRounds[resultRounds.Count - 1].NpcEconomicScoreAfterRound,
            resultRounds[resultRounds.Count - 1].TargetScoreAfterRound,
            resultRounds[resultRounds.Count - 1].InterestRateAfterRound,
            resultRounds.AsReadOnly());

        smallPanelResults.Add(LatestSmallPanelResult);
        pendingSmallPanelRounds.Clear();
        OnSmallPanelResultReady?.Invoke(LatestSmallPanelResult);
    }

    private void CompleteBigPanelResult(string endReason)
    {
        IsBigGameEnded = true;
        ShouldPauseForSmallPanel = false;
        int bigRoundScore = allRoundResults.Count > 0 ? allRoundResults[allRoundResults.Count - 1].PlayerScore : PlayerTotalScore;
        int finalSettlementAmount = bigRoundScore - TargetScore;
        IsPlayerWin = endReason != "RoundSettlementNegative" && finalSettlementAmount >= 0;

        LatestBigPanelResult = new BigPanelResultData(
            bigRoundScore,
            NpcEconomicScore,
            TargetScore,
            CurrentInterestRate,
            BaseInterestRate,
            CheatInterestRate,
            LoanAmount,
            bigRoundScore,
            finalSettlementAmount,
            IsPlayerWin,
            endReason,
            new List<EconomyRoundResultData>(allRoundResults).AsReadOnly(),
            new List<SmallPanelResultData>(smallPanelResults).AsReadOnly());

        OnBigPanelResultReady?.Invoke(LatestBigPanelResult);

        if (autoShowBigPanel)
            ShowPanelByName(IsPlayerWin ? bigSuccessPanelName : bigFailPanelName, LatestBigPanelResult);
    }

    public void ShowLatestBigPanelResult()
    {
        if (LatestBigPanelResult == null)
            return;

        ShowPanelByName(IsPlayerWin ? bigSuccessPanelName : bigFailPanelName, LatestBigPanelResult);
    }

    private float CalculateCurrentInterestRate()
    {
        return BaseInterestRate + cheatInterestAdd * cheatedRoundCount;
    }

    private int CalculateBetAmount(int availableAmount)
    {
        return Mathf.Min(Mathf.Max(0, availableAmount), CurrentBetLimit);
    }

    private int CalculateInterestAmount(float interestRate)
    {
        return Mathf.RoundToInt(loanAmount * interestRate);
    }

    private int GetCurrentBetLimit()
    {
        int levelIndex = GetLevelIndex();
        if (levelIndex <= 0)
            return firstLevelBetLimit;
        if (levelIndex == 1)
            return secondLevelBetLimit;

        return thirdLevelBetLimit;
    }

    private float GetCurrentBaseInterestRate()
    {
        int levelIndex = GetLevelIndex();
        if (levelIndex <= 0)
            return firstLevelInterestRate;
        if (levelIndex == 1)
            return secondLevelInterestRate;

        return thirdLevelInterestRate;
    }

    private int GetLevelIndex()
    {
        return cardManager != null ? cardManager.Level_index : 0;
    }

    private void ShowPanelByName(string panelTypeName, object data)
    {
        if (string.IsNullOrEmpty(panelTypeName))
            return;

        Type panelType = FindPanelType(panelTypeName);
        if (panelType == null || !typeof(BasePanel).IsAssignableFrom(panelType))
        {
            Debug.LogWarning("[GameEconomyBridge] Panel type not found: " + panelTypeName);
            return;
        }

        MethodInfo showPanelMethod = typeof(UIManager).GetMethod("ShowPanel");
        MethodInfo genericShowPanel = showPanelMethod.MakeGenericMethod(panelType);
        BasePanel panel = genericShowPanel.Invoke(UIManager.Instance, null) as BasePanel;

        if (panel is ISmallEndPanelDataReceiver smallReceiver && data is SmallPanelResultData smallData)
            smallReceiver.SetData(smallData);

        if (panel is IBigEndPanelDataReceiver bigReceiver && data is BigPanelResultData bigData)
            bigReceiver.SetData(bigData);
    }

    private Type FindPanelType(string panelTypeName)
    {
        Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
        foreach (Assembly assembly in assemblies)
        {
            Type[] types;
            try
            {
                types = assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                types = ex.Types;
            }

            foreach (Type type in types)
            {
                if (type != null && type.Name == panelTypeName)
                    return type;
            }
        }

        return null;
    }
}
