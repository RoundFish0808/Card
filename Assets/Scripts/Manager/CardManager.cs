using System.Collections.Generic;
using UnityEngine;

public class CardManager : MonoBehaviour
{
    [Header("所有卡牌数据，放 52 个 CardDataSO")]
    public List<CardDataSO> allCards = new List<CardDataSO>();

    [Header("通用卡牌预制体")]
    public GameObject cardPrefab;

    [Header("玩家手牌卡槽")]
    public Transform[] playerCardSlots;

    private List<CardDataSO> deck = new List<CardDataSO>();
    private List<CardDataSO> playerHand = new List<CardDataSO>();

    private void Start()
    {
        CreateDeck();
        ShuffleDeck();

        DrawPlayerCards(3);
        ShowPlayerCards();
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

        Debug.Log("牌堆生成完成，数量：" + deck.Count);
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

    private void ShowPlayerCards()
    {
        if (cardPrefab == null)
        {
            Debug.LogError("CardManager 没有绑定 cardPrefab");
            return;
        }

        if (playerCardSlots == null || playerCardSlots.Length < 3)
        {
            Debug.LogError("玩家手牌卡槽数量不足 3 个");
            return;
        }

        for (int i = 0; i < playerHand.Count; i++)
        {
            Transform slot = playerCardSlots[i];

            if (slot == null)
            {
                Debug.LogError("第 " + i + " 个玩家手牌卡槽为空");
                continue;
            }

            GameObject cardObj = Instantiate(cardPrefab, slot);

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
}