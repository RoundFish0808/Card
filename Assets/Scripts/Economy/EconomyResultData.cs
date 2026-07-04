using System.Collections.Generic;

public sealed class EconomyRoundResultData
{
    public int RoundIndex { get; }
    public int PlayerScore { get; }
    public int SmallRoundScore => PlayerScore;
    public int PlayerTotalScore { get; }
    public int PrincipalBeforeRound { get; }
    public int BetAmount => PrincipalBeforeRound;
    public int AvailableAmountBeforeRound { get; }
    public int BetLimit { get; }
    public int WinDiffScore { get; }
    public int FormulaBase { get; }
    public int NpcBattleScore { get; }
    public int NpcBattleTotalScore { get; }
    public string HandTypeName { get; }
    public float HandMultiplier { get; }
    public bool IsCheated { get; }
    public float InterestRateAfterRound { get; }
    public float SmallRoundInterestRate => InterestRateAfterRound;
    public int NpcEconomicScoreAfterRound { get; }
    public int InterestAmountAfterRound => NpcEconomicScoreAfterRound;
    public int TargetScoreAfterRound => NpcEconomicScoreAfterRound;
    public int SettlementAmountAfterRound { get; }

    public EconomyRoundResultData(
        int roundIndex,
        int playerScore,
        int playerTotalScore,
        int principalBeforeRound,
        int availableAmountBeforeRound,
        int betLimit,
        int winDiffScore,
        int formulaBase,
        int npcBattleScore,
        int npcBattleTotalScore,
        string handTypeName,
        float handMultiplier,
        bool isCheated,
        float interestRateAfterRound,
        int npcEconomicScoreAfterRound,
        int settlementAmountAfterRound)
    {
        RoundIndex = roundIndex;
        PlayerScore = playerScore;
        PlayerTotalScore = playerTotalScore;
        PrincipalBeforeRound = principalBeforeRound;
        AvailableAmountBeforeRound = availableAmountBeforeRound;
        BetLimit = betLimit;
        WinDiffScore = winDiffScore;
        FormulaBase = formulaBase;
        NpcBattleScore = npcBattleScore;
        NpcBattleTotalScore = npcBattleTotalScore;
        HandTypeName = handTypeName;
        HandMultiplier = handMultiplier;
        IsCheated = isCheated;
        InterestRateAfterRound = interestRateAfterRound;
        NpcEconomicScoreAfterRound = npcEconomicScoreAfterRound;
        SettlementAmountAfterRound = settlementAmountAfterRound;
    }
}

public sealed class SmallPanelResultData
{
    public int SmallIndex { get; }
    public int StartRoundIndex { get; }
    public int EndRoundIndex { get; }
    public int PlayerSmallScore { get; }
    public int SmallRoundScore => PlayerSmallScore;
    public int NpcScore { get; }
    public int NpcEconomicScore => NpcScore;
    public int TargetScore { get; }
    public float SmallInterestRate { get; }
    public float SmallRoundInterest => SmallInterestRate;
    public IReadOnlyList<string> HandTypeNames { get; }
    public IReadOnlyList<EconomyRoundResultData> RoundResults { get; }

    public SmallPanelResultData(
        int smallIndex,
        int startRoundIndex,
        int endRoundIndex,
        int playerSmallScore,
        int npcScore,
        int targetScore,
        float smallInterestRate,
        IReadOnlyList<EconomyRoundResultData> roundResults)
    {
        SmallIndex = smallIndex;
        StartRoundIndex = startRoundIndex;
        EndRoundIndex = endRoundIndex;
        PlayerSmallScore = playerSmallScore;
        NpcScore = npcScore;
        TargetScore = targetScore;
        SmallInterestRate = smallInterestRate;
        RoundResults = roundResults;

        List<string> handTypeNames = new List<string>();
        if (roundResults != null)
        {
            foreach (EconomyRoundResultData result in roundResults)
                handTypeNames.Add(result.HandTypeName);
        }

        HandTypeNames = handTypeNames.AsReadOnly();
    }
}

public sealed class BigPanelResultData
{
    public int PlayerTotalScore { get; }
    public int BigGameAmount => BigRoundScore;
    public int NpcScore { get; }
    public int NpcEconomicScore => NpcScore;
    public int TargetScore { get; }
    public float FinalInterestRate { get; }
    public float BaseInterestRate { get; }
    public float CheatInterestRate { get; }
    public int LoanAmount { get; }
    public int BigRoundScore { get; }
    public int FinalSettlementAmount { get; }
    public bool IsPlayerWin { get; }
    public string EndReason { get; }
    public IReadOnlyList<EconomyRoundResultData> AllRoundResults { get; }
    public IReadOnlyList<SmallPanelResultData> SmallPanelResults { get; }

    public BigPanelResultData(
        int playerTotalScore,
        int npcScore,
        int targetScore,
        float finalInterestRate,
        float baseInterestRate,
        float cheatInterestRate,
        int loanAmount,
        int bigRoundScore,
        int finalSettlementAmount,
        bool isPlayerWin,
        string endReason,
        IReadOnlyList<EconomyRoundResultData> allRoundResults,
        IReadOnlyList<SmallPanelResultData> smallPanelResults)
    {
        PlayerTotalScore = playerTotalScore;
        NpcScore = npcScore;
        TargetScore = targetScore;
        FinalInterestRate = finalInterestRate;
        BaseInterestRate = baseInterestRate;
        CheatInterestRate = cheatInterestRate;
        LoanAmount = loanAmount;
        BigRoundScore = bigRoundScore;
        FinalSettlementAmount = finalSettlementAmount;
        IsPlayerWin = isPlayerWin;
        EndReason = endReason;
        AllRoundResults = allRoundResults;
        SmallPanelResults = smallPanelResults;
    }
}

public interface ISmallEndPanelDataReceiver
{
    void SetData(SmallPanelResultData data);
}

public interface IBigEndPanelDataReceiver
{
    void SetData(BigPanelResultData data);
}
