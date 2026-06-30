using System.Collections.Generic;
using UnityEngine;

public class CardManager : MonoBehaviour
{
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
    public bool debugShowNpcCards = true;
    public bool autoDeclareOnStart = true;
    public float openingDeclarationDisplayTime = 5f;

    [Header("是否使用弧形手牌")]
    public bool useArcHand = true;

    private List<CardDataSO> deck = new List<CardDataSO>();
    private List<CardDataSO> playerHand = new List<CardDataSO>();
    private List<CardDataSO> npcHand = new List<CardDataSO>();
    private List<GameObject> playerHandObjects = new List<GameObject>();

    private void Start()
    {
        CreateDeck();
        ShuffleDeck();

        DrawPlayerCards(5);
        DrawNPCCards(3);
        ShowPlayerCards();
        ShowNPCCards();
        ShowOpeningNPCDeclaration();
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
            int randomIndex = Random.Range(i, deck.Count);

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

        Debug.Log("NPC 初始手牌：" + FormatCardList(npcHand));
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
        if (!autoDeclareOnStart) return;

        if (npcPresenter == null)
        {
            Debug.LogWarning("CardManager 没有找到 NPCPresenter，无法生成开局宣言");
            return;
        }

        if (npcHand.Count < 3)
        {
            Debug.LogWarning("NPC 初始手牌不足 3 张，无法生成开局宣言");
            return;
        }

        npcPresenter.GenerateAndShowDeclaration(openingDeclarationDisplayTime);
        Debug.Log("NPC 已根据开局手牌生成宣言并显示");
    }

    public void SetNPCDebugCardsVisible(bool visible)
    {
        debugShowNpcCards = visible;

        if (npcPresenter == null)
            npcPresenter = FindObjectOfType<NPCPresenter>();

        if (npcPresenter != null)
            npcPresenter.SetDebugShowFront(visible);
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
            cardView.Init(card);

        if (useArcHand && handController != null)
        {
            handController.AddCard(cardObj);
        }
    }
}
