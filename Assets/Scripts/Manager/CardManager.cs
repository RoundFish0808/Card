using System.Collections.Generic;
using System;
using System.Collections;
using UnityEngine;

public class CardManager : MonoBehaviour
{
    private const int PlayerCardsPerSmallRound = 4;
    private const int NPCCardsPerSmallRound = 3;

    [Header("全部卡牌数据（共 52 张 CardDataSO）")]
    public List<CardDataSO> allCards = new List<CardDataSO>();

    [Header("通用卡牌预制体")]
    public GameObject cardPrefab;

    [Header("玩家卡牌槽位（旧版水平排列）")]
    public Transform[] playerCardSlots;

    [Header("弧形手牌控制器")]
    [Tooltip("使用样条曲线思路实现的弧形手牌控制器")]
    public HandController handController;

    [Header("NPC 手牌控制")]
    public NPCPresenter npcPresenter;
    public bool debugShowNpcCards = false;
    public bool autoDeclareOnStart = true;
    public float openingDeclarationDisplayTime = 5f;
    public bool autoPlaceNpcCardsToSlots = true;

    [Header("战场流程")]
    public BattleFieldView battleField;
    public AttackSystem attackSystem;
    public bool enableGameFlow = true;

    [Header("抽牌动画")]
    public RectTransform playerDrawPileTransform;
    public float drawToHandDuration = 0.45f;

    [Header("是否使用弧形手牌")]
    public bool useArcHand = true;

    private List<CardDataSO> deck = new List<CardDataSO>();
    private List<CardDataSO> playerHand = new List<CardDataSO>();
    private List<CardDataSO> npcHand = new List<CardDataSO>();
    private List<GameObject> playerHandObjects = new List<GameObject>();
    private readonly List<CardDataSO> discardPile = new List<CardDataSO>();
    private readonly List<CardDataSO> currentSmallRoundPlayerCards = new List<CardDataSO>();
    private readonly IBattleCalculator battleCalculator = new StandardBattleCalculator();

    private int playerTotalScore;
    private int npcTotalScore;
    private int currentSmallRoundPlayerRawScore;
    private int currentSmallRoundNpcScore;
    private int turnIndex;
    private int smallRoundIndex;
    private bool gameEnded;
    private bool currentSmallRoundHasDrawnAttackReward;

    public event Action<BattleResultData> OnTurnScored;
    public event Action<int, int> OnTotalScoreChanged;
    public event Action<int, int, BattleResult> OnGameEnded;

    private void Start()
    {
        ResolveRuntimeReferences();
        CreateDeck();
        ShuffleDeck();

        DrawPlayerCards(PlayerCardsPerSmallRound);
        ShowPlayerCards();

        if (enableGameFlow)
            StartNextSmallRound();
    }

    private void OnDestroy()
    {
        if (attackSystem != null)
            attackSystem.OnAttackResolved -= HandleAttackResolved;
    }

    private void ResolveRuntimeReferences()
    {
        if (battleField == null)
            battleField = FindObjectOfType<BattleFieldView>();

        if (attackSystem == null)
            attackSystem = FindObjectOfType<AttackSystem>();

        if (npcPresenter == null)
            npcPresenter = FindObjectOfType<NPCPresenter>();

        if (attackSystem != null)
        {
            attackSystem.SetBattleCalculator(battleCalculator);
            attackSystem.OnAttackResolved -= HandleAttackResolved;
            attackSystem.OnAttackResolved += HandleAttackResolved;
        }
    }

    private void CreateDeck()
    {
        deck.Clear();

        foreach (CardDataSO card in allCards)
        {
            if (card != null)
            {
                deck.Add(card);
            }
        }

        Debug.Log("牌堆创建完成，总牌数：" + deck.Count);
    }

    private void ShuffleDeck()
    {
        for (int i = 0; i < deck.Count; i++)
        {
            int randomIndex = UnityEngine.Random.Range(i, deck.Count);

            CardDataSO temp = deck[i];
            deck[i] = deck[randomIndex];
            deck[randomIndex] = temp;
        }
    }

    private CardDataSO DrawCard()
    {
        if (deck.Count <= 0)
        {
            Debug.LogWarning("牌堆空了");
            return null;
        }

        CardDataSO card = deck[0];
        deck.RemoveAt(0);

        return card;
    }

    private void DrawPlayerCards(int count)
    {
        playerHand.Clear();

        for (int i = 0; i < count; i++)
        {
            CardDataSO card = DrawCard();

            if (card != null)
            {
                playerHand.Add(card);
                Debug.Log("玩家抽到：" + card.cardName);
            }
        }
    }

    private void DrawNPCCards(int count)
    {
        npcHand.Clear();

        for (int i = 0; i < count; i++)
        {
            CardDataSO card = DrawCard();

            if (card != null)
            {
                npcHand.Add(card);
            }
        }

        Debug.Log("NPC 本小局手牌：" + FormatCardList(npcHand));
    }

    private void ShowNPCCards()
    {
        if (npcPresenter == null)
        {
            npcPresenter = FindObjectOfType<NPCPresenter>();
        }

        if (npcPresenter == null)
        {
            Debug.LogWarning("CardManager 没有找到 NPCPresenter，无法显示 NPC 初始手牌");
            return;
        }

        npcPresenter.debugShowFront = debugShowNpcCards;
        npcPresenter.InitializeHand(npcHand);
        npcPresenter.SetDebugShowFront(debugShowNpcCards);
        Debug.Log($"NPC 手牌显示完成，数量：{npcHand.Count}，Debug正面显示：{debugShowNpcCards}");
    }

    private void ShowOpeningNPCDeclaration()
    {
        if (npcPresenter == null)
        {
            Debug.LogWarning("CardManager 没有找到 NPCPresenter，无法生成开局宣言");
            return;
        }

        if (npcHand.Count < NPCCardsPerSmallRound)
        {
            Debug.LogWarning($"NPC 初始手牌不足 {NPCCardsPerSmallRound} 张，无法生成开局宣言");
            return;
        }

        if (autoDeclareOnStart)
        {
            npcPresenter.GenerateAndShowDeclaration(openingDeclarationDisplayTime);
            Debug.Log("NPC 已根据开局手牌生成宣言并显示");
        }

        if (autoPlaceNpcCardsToSlots)
        {
            npcPresenter.PlaceArrangedCardsToNPCSlots();
            Debug.Log("NPC 已按布阵结果放置到 Canvas/NpcSlots");
        }
    }

    public void SetNPCDebugCardsVisible(bool visible)
    {
        debugShowNpcCards = visible;

        if (npcPresenter == null)
            npcPresenter = FindObjectOfType<NPCPresenter>();

        if (npcPresenter != null)
            npcPresenter.SetDebugShowFront(visible);
    }

    private void StartNextSmallRound()
    {
        if (gameEnded) return;

        if (npcPresenter == null || battleField == null)
        {
            EndGame("NPC 或战场引用缺失，无法开始小局");
            return;
        }

        int currentPlayerHandCount = GetCurrentPlayerHandCount();
        if (currentPlayerHandCount > 0 && currentPlayerHandCount < PlayerCardsPerSmallRound)
        {
            EndGame($"玩家手牌不足 {PlayerCardsPerSmallRound} 张，无法开始下一小局");
            return;
        }

        int requiredPlayerCards = currentPlayerHandCount == 0 ? PlayerCardsPerSmallRound : 0;
        int requiredCards = NPCCardsPerSmallRound + requiredPlayerCards;
        if (deck.Count < requiredCards)
        {
            EndGame($"牌库不足以开始下一小局：需要 NPC {NPCCardsPerSmallRound} 张 + 玩家 {requiredPlayerCards} 张，当前牌库 {deck.Count} 张");
            return;
        }

        smallRoundIndex++;
        currentSmallRoundPlayerRawScore = 0;
        currentSmallRoundNpcScore = 0;
        currentSmallRoundPlayerCards.Clear();
        currentSmallRoundHasDrawnAttackReward = false;

        DrawNPCCards(NPCCardsPerSmallRound);
        ShowNPCCards();
        ShowOpeningNPCDeclaration();

        if (GetCurrentPlayerHandCount() == 0)
        {
            DrawPlayerCards(PlayerCardsPerSmallRound);
            ShowPlayerCards();
        }

        if (GetCurrentPlayerHandCount() < PlayerCardsPerSmallRound)
        {
            EndGame($"玩家手牌不足 {PlayerCardsPerSmallRound} 张，无法开始下一小局");
            return;
        }

        UpdateBattleModeAfterFlowChange();

        Debug.Log($"[小局开始] 第 {smallRoundIndex} 小局，剩余牌库：{deck.Count}");
    }

    private void HandleAttackResolved(CardView attacker, CardView target, SlotView playerSlot, SlotView npcSlot, BattleResultData result)
    {
        if (gameEnded || attacker == null || target == null || playerSlot == null || npcSlot == null)
            return;

        turnIndex++;

        currentSmallRoundPlayerCards.Add(attacker.CardData);
        currentSmallRoundPlayerRawScore += result.playerScore;
        currentSmallRoundNpcScore += result.npcScore;

        OnTurnScored?.Invoke(result);

        Debug.Log($"[回合 {turnIndex}] 玩家 {attacker.CardData.cardName} vs NPC {target.CardData.cardName}，玩家本回合+{result.playerScore}，NPC本回合+{result.npcScore}");

        playerSlot.SetResolvedThisRound(true);
        npcSlot.SetResolvedThisRound(true);

        // 每个小局只有第一次攻击完成后奖励摸 1 张，后续攻击不再摸。
        if (!currentSmallRoundHasDrawnAttackReward)
        {
            currentSmallRoundHasDrawnAttackReward = true;
            DrawNewCard();
        }

        if (CountAvailableNPCSlotsForAttack() == 0)
        {
            CompleteSmallRound();
            return;
        }

        if (!CanPlayerContinue())
        {
            EndGame("玩家已无可用卡牌且牌库耗尽，无法继续攻击");
            return;
        }

        UpdateBattleModeAfterFlowChange();
    }

    private void DiscardAllSlotCards()
    {
        DiscardSlots(battleField != null ? battleField.playerSlots : null, "玩家");
        DiscardSlots(battleField != null ? battleField.npcSlots : null, "NPC");
        Debug.Log($"[弃牌堆] 当前弃牌数量：{discardPile.Count}");
    }

    private void DiscardSlots(SlotView[] slots, string ownerName)
    {
        if (slots == null) return;

        foreach (var slot in slots)
        {
            if (slot == null || !slot.IsOccupied) continue;

            CardView removed = slot.RemoveCard();
            if (removed != null)
            {
                if (removed.CardData != null)
                    discardPile.Add(removed.CardData);

                Debug.Log($"[弃牌] {ownerName}槽位 {slot.slotIndex}: {(removed.CardData != null ? removed.CardData.cardName : "空牌")}");
                Destroy(removed.gameObject);
            }
        }
    }

    private void ReturnPlayerHandToDeck()
    {
        int returnedCount = 0;

        if (handController != null)
        {
            var handCardsSnapshot = new List<GameObject>(handController.Cards);
            foreach (var cardObj in handCardsSnapshot)
            {
                if (cardObj == null) continue;

                CardView cardView = cardObj.GetComponent<CardView>();
                if (cardView != null && cardView.CardData != null)
                {
                    deck.Add(cardView.CardData);
                    returnedCount++;
                }
            }

            handController.ClearCards();
        }

        playerHand.Clear();
        ShuffleDeck();

        Debug.Log($"[回牌堆] 玩家未打出的手牌回到抽牌堆：{returnedCount} 张，当前牌库：{deck.Count}");
    }

    private void CompleteSmallRound()
    {
        float multiplier = ScoreMultiplierRules.GetMultiplier(currentSmallRoundPlayerCards, out string comboName);
        int finalPlayerScore = Mathf.RoundToInt(currentSmallRoundPlayerRawScore * multiplier);

        playerTotalScore += finalPlayerScore;
        npcTotalScore += currentSmallRoundNpcScore;

        Debug.Log($"[小局结算] 第 {smallRoundIndex} 小局：玩家原始分={currentSmallRoundPlayerRawScore}，牌型={comboName}，倍率=x{multiplier}，玩家入账={finalPlayerScore}，NPC入账={currentSmallRoundNpcScore}");
        Debug.Log($"[总分] 玩家={playerTotalScore}，NPC={npcTotalScore}");

        OnTotalScoreChanged?.Invoke(playerTotalScore, npcTotalScore);

        DiscardAllSlotCards();
        ReturnPlayerHandToDeck();

        int requiredCardsForNextSmallRound = NPCCardsPerSmallRound + PlayerCardsPerSmallRound;
        if (deck.Count < requiredCardsForNextSmallRound)
        {
            EndGame($"下一小局无法完整开始：NPC 需要 {NPCCardsPerSmallRound} 张，玩家需要 {PlayerCardsPerSmallRound} 张，当前牌库 {deck.Count} 张");
            return;
        }

        StartNextSmallRound();
    }

    private bool CanPlayerContinue()
    {
        return CountAvailablePlayerSlotsForAttack() > 0 || GetCurrentPlayerHandCount() > 0;
    }

    private int CountOccupiedPlayerSlots()
    {
        if (battleField == null || battleField.playerSlots == null) return 0;

        int count = 0;
        foreach (var slot in battleField.playerSlots)
        {
            if (slot != null && slot.IsOccupied)
                count++;
        }

        return count;
    }

    private int CountOccupiedNPCSlots()
    {
        if (battleField == null || battleField.npcSlots == null) return 0;

        int count = 0;
        foreach (var slot in battleField.npcSlots)
        {
            if (slot != null && slot.IsOccupied)
                count++;
        }

        return count;
    }

    private int CountAvailablePlayerSlotsForAttack()
    {
        return CountAvailableSlotsForAttack(battleField != null ? battleField.playerSlots : null);
    }

    private int CountAvailableNPCSlotsForAttack()
    {
        return CountAvailableSlotsForAttack(battleField != null ? battleField.npcSlots : null);
    }

    private int CountAvailableSlotsForAttack(SlotView[] slots)
    {
        if (slots == null) return 0;

        int count = 0;
        foreach (var slot in slots)
        {
            if (slot != null && slot.IsOccupied && !slot.IsResolvedThisRound)
                count++;
        }

        return count;
    }

    private int GetCurrentPlayerHandCount()
    {
        if (handController != null)
            return handController.CardCount;

        return playerHand.Count;
    }

    private void UpdateBattleModeAfterFlowChange()
    {
        if (battleField == null) return;

        bool canAttack = CountAvailablePlayerSlotsForAttack() > 0 && CountAvailableNPCSlotsForAttack() > 0;
        battleField.SetMode(!canAttack);
    }

    private void EndGame(string reason)
    {
        if (gameEnded) return;

        gameEnded = true;
        BattleResult finalResult = BattleResult.Draw;
        if (playerTotalScore > npcTotalScore)
            finalResult = BattleResult.Win;
        else if (playerTotalScore < npcTotalScore)
            finalResult = BattleResult.Lose;

        if (battleField != null)
            battleField.SetMode(true);

        Debug.Log($"[大局结算] {reason}。最终比分：玩家={playerTotalScore}，NPC={npcTotalScore}，结果={finalResult}");
        OnGameEnded?.Invoke(playerTotalScore, npcTotalScore, finalResult);
    }

    private string FormatCardList(List<CardDataSO> cards)
    {
        if (cards == null || cards.Count == 0)
            return "无";

        List<string> names = new List<string>();
        foreach (var card in cards)
        {
            names.Add(card != null ? card.cardName : "空牌");
        }

        return string.Join("、", names);
    }

    private void ShowPlayerCards()
    {
        if (cardPrefab == null)
        {
            Debug.LogError("CardManager 没有挂 cardPrefab");
            return;
        }

        ClearPlayerHandObjects();

        if (useArcHand && handController != null)
        {
            ShowPlayerCards_Arc();
        }
        else
        {
            ShowPlayerCards_Slots();
        }
    }

    private void ShowPlayerCards_Arc()
    {
        for (int i = 0; i < playerHand.Count; i++)
        {
            GameObject cardObj = Instantiate(cardPrefab);
            playerHandObjects.Add(cardObj);

            CardView cardView = cardObj.GetComponent<CardView>();

            if (cardView == null)
            {
                Debug.LogError("CardPrefab 上没有挂 CardView 脚本");
                continue;
            }

            cardView.Init(playerHand[i]);
            EnsurePlayerCardInteraction(cardObj, cardView);
            handController.AddCard(cardObj);
        }

        Debug.Log("弧形手牌显示完成，剩余牌堆：" + deck.Count);
    }

    private void ShowPlayerCards_Slots()
    {
        if (playerCardSlots == null || playerCardSlots.Length < playerHand.Count)
        {
            Debug.LogError("玩家卡牌槽位数量不足");
            return;
        }

        for (int i = 0; i < playerHand.Count; i++)
        {
            Transform slot = playerCardSlots[i];

            if (slot == null)
            {
                Debug.LogError("第 " + i + " 个卡牌槽位为空");
                continue;
            }

            GameObject cardObj = Instantiate(cardPrefab, slot);
            playerHandObjects.Add(cardObj);

            RectTransform rect = cardObj.GetComponent<RectTransform>();

            if (rect != null)
            {
                rect.anchoredPosition = Vector2.zero;
                rect.localRotation = Quaternion.identity;
                rect.localScale = Vector3.one;
            }

            CardView cardView = cardObj.GetComponent<CardView>();

            if (cardView == null)
            {
                Debug.LogError("CardPrefab 上没有挂 CardView 脚本");
                continue;
            }

            cardView.Init(playerHand[i]);
        }

        Debug.Log("玩家手牌显示完成，剩余牌堆：" + deck.Count);
    }

    private void ClearPlayerHandObjects()
    {
        if (useArcHand && handController != null)
        {
            handController.ClearCards();
        }

        foreach (var obj in playerHandObjects)
        {
            if (obj != null)
                Destroy(obj);
        }
        playerHandObjects.Clear();
    }

    public void PlayCardFromHand(int index)
    {
        if (index < 0 || index >= playerHand.Count) return;

        playerHand.RemoveAt(index);

        if (useArcHand && handController != null && index < playerHandObjects.Count)
        {
            GameObject cardToRemove = playerHandObjects[index];
            playerHandObjects.RemoveAt(index);
            handController.RemoveCard(cardToRemove);
            Destroy(cardToRemove);
        }
    }

    public void DrawNewCard()
    {
        CardDataSO card = DrawCard();
        if (card == null) return;

        playerHand.Add(card);

        GameObject cardObj = Instantiate(cardPrefab);
        playerHandObjects.Add(cardObj);

        CardView cardView = cardObj.GetComponent<CardView>();
        if (cardView != null)
        {
            cardView.Init(card);
            EnsurePlayerCardInteraction(cardObj, cardView);
        }

        if (useArcHand && handController != null)
        {
            float oldDuration = handController.layoutAnimationDuration;
            float drawDuration = Mathf.Max(0.01f, drawToHandDuration);
            handController.layoutAnimationDuration = drawDuration;

            PrepareDrawAnimationStart(cardObj);
            handController.AddCard(cardObj);
            StartCoroutine(RestoreHandLayoutDurationAfterDraw(oldDuration, drawDuration));
        }
    }

    private void PrepareDrawAnimationStart(GameObject cardObj)
    {
        if (cardObj == null || handController == null) return;

        RectTransform cardRect = cardObj.GetComponent<RectTransform>();
        RectTransform handRect = handController.GetComponent<RectTransform>();
        if (cardRect == null || handRect == null) return;

        cardRect.SetParent(handController.transform, false);

        // 摸牌动画起点：优先从配置的抽牌堆位置飞入；未配置时从手牌区上方缓慢进入。
        if (playerDrawPileTransform != null)
        {
            cardRect.localPosition = handRect.InverseTransformPoint(playerDrawPileTransform.position);
        }
        else
        {
            cardRect.localPosition = new Vector3(0f, 260f, 0f);
        }

        cardRect.localScale = Vector3.zero;
    }

    private IEnumerator RestoreHandLayoutDurationAfterDraw(float oldDuration, float waitTime)
    {
        if (handController == null) yield break;

        yield return new WaitForSeconds(waitTime);

        if (handController != null)
            handController.layoutAnimationDuration = oldDuration;
    }

    private void EnsurePlayerCardInteraction(GameObject cardObj, CardView cardView)
    {
        if (cardObj == null || cardView == null) return;

        cardView.isPlayerCard = true;
        cardView.Reveal();

        if (cardObj.GetComponent<CardDragHandler>() == null)
            cardObj.AddComponent<CardDragHandler>();
    }
}
