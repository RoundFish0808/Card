using UnityEngine;
using System.Collections.Generic;
using System;

public class HandPresenter : MonoBehaviour
{
    [Header("视图")]
    public HandController handController;

    [Header("卡牌预制体")]
    public GameObject cardPrefab;

    [Header("战场视图")]
    public BattleFieldView battleField;

    [Header("抽牌动画起始点 (牌堆位置)")]
    public RectTransform drawPileTransform;

    [Header("单张抽牌动画时间")]
    public float drawCardDuration = 0.4f;

    [Header("抽牌间隔")]
    public float drawInterval = 0.1f;

    private readonly List<CardView> cardViews = new List<CardView>();
    private CardView selectedCard;

    public event Action<CardView> OnCardSelected;
    public event Action<CardView, int> OnCardPlacedToSlot;

    private void Awake()
    {
        if (handController != null)
        {
            handController.OnCardClicked += HandleCardClicked;
            handController.OnCardPointerEnter += HandleCardPointerEnter;
            handController.OnCardPointerExit += HandleCardPointerExit;
        }

        if (battleField != null)
        {
            battleField.OnPlayerPlacedCard += HandlePlacedToSlot;
        }
    }

    private void OnDestroy()
    {
        if (handController != null)
        {
            handController.OnCardClicked -= HandleCardClicked;
            handController.OnCardPointerEnter -= HandleCardPointerEnter;
            handController.OnCardPointerExit -= HandleCardPointerExit;
        }

        if (battleField != null)
        {
            battleField.OnPlayerPlacedCard -= HandlePlacedToSlot;
        }
    }

    public void DrawCards(List<CardDataSO> cardDataList, bool withAnimation = true)
    {
        if (cardDataList == null || cardDataList.Count == 0) return;

        if (withAnimation && drawPileTransform != null)
        {
            StartCoroutine(DrawCardsAnimated(cardDataList));
        }
        else
        {
            foreach (var data in cardDataList)
            {
                CreateCard(data);
            }
            handController.RefreshLayout();
        }
    }

    public void DrawCard(CardDataSO cardData, bool withAnimation = true)
    {
        if (withAnimation && drawPileTransform != null)
        {
            StartCoroutine(DrawCardAnimated(cardData));
        }
        else
        {
            CreateCard(cardData);
            handController.RefreshLayout();
        }
    }

    private void CreateCard(CardDataSO data)
    {
        if (cardPrefab == null || handController == null) return;

        GameObject cardObj = Instantiate(cardPrefab);
        CardView cardView = cardObj.GetComponent<CardView>();
        if (cardView == null)
            cardView = cardObj.AddComponent<CardView>();

        cardView.Init(data);
        cardView.Reveal();

        cardViews.Add(cardView);
        handController.AddCard(cardObj, false);
    }

    private System.Collections.IEnumerator DrawCardsAnimated(List<CardDataSO> cardDataList)
    {
        foreach (var data in cardDataList)
        {
            StartCoroutine(DrawCardAnimated(data));
            yield return new WaitForSeconds(drawInterval);
        }
    }

    private System.Collections.IEnumerator DrawCardAnimated(CardDataSO data)
    {
        if (cardPrefab == null || handController == null) yield break;

        GameObject cardObj = Instantiate(cardPrefab);
        RectTransform rect = cardObj.GetComponent<RectTransform>();
        if (rect == null) rect = cardObj.AddComponent<RectTransform>();

        CardView cardView = cardObj.GetComponent<CardView>();
        if (cardView == null)
            cardView = cardObj.AddComponent<CardView>();

        cardView.Init(data);
        cardView.Hide();

        rect.SetParent(handController.transform, false);
        rect.position = drawPileTransform.position;
        rect.localScale = Vector3.zero;

        cardViews.Add(cardView);
        handController.AddCard(cardObj, false);

        float t = 0f;
        Vector3 startPos = rect.position;
        cardView.Reveal();

        while (t < drawCardDuration)
        {
            t += Time.deltaTime;
            float p = t / drawCardDuration;
            float eased = 1f - Mathf.Pow(1f - p, 3f);
            rect.localScale = Vector3.Lerp(Vector3.zero, Vector3.one, eased);
            yield return null;
        }

        rect.localScale = Vector3.one;
        handController.RefreshLayout();
    }

    public void RemoveCard(CardView card)
    {
        if (card == null) return;

        cardViews.Remove(card);
        handController.RemoveCard(card.gameObject);
    }

    private void HandleCardClicked(CardView card)
    {
        if (selectedCard == card)
        {
            selectedCard.SetSelected(false);
            selectedCard = null;
            if (battleField != null)
                battleField.SetSelectedCard(null);
        }
        else
        {
            if (selectedCard != null)
                selectedCard.SetSelected(false);

            selectedCard = card;
            selectedCard.SetSelected(true);
            if (battleField != null)
                battleField.SetSelectedCard(card);

            OnCardSelected?.Invoke(card);
        }
    }

    private void HandleCardPointerEnter(CardView card)
    {
    }

    private void HandleCardPointerExit(CardView card)
    {
    }

    private void HandlePlacedToSlot(int slotIndex, CardView card)
    {
        if (cardViews.Contains(card))
        {
            cardViews.Remove(card);
            handController.RemoveCard(card.gameObject);
        }

        if (selectedCard == card)
        {
            selectedCard.SetSelected(false);
            selectedCard = null;
            if (battleField != null)
                battleField.SetSelectedCard(null);
        }

        OnCardPlacedToSlot?.Invoke(card, slotIndex);
    }

    public int CardCount => cardViews.Count;

    public List<CardView> GetAllCards()
    {
        return new List<CardView>(cardViews);
    }

    public CardView GetCardAt(int index)
    {
        if (index < 0 || index >= cardViews.Count) return null;
        return cardViews[index];
    }
}
