using UnityEngine;
using System.Collections.Generic;
using System;

public class HandPresenter : MonoBehaviour
{
    [Header("视图")]
    public HandController handController;

    [Header("战场视图")]
    public BattleFieldView battleField;

    [Header("攻击系统")]
    public AttackSystem attackSystem;

    [Header("卡牌预制体")]
    public GameObject cardPrefab;

    [Header("抽牌动画起始点 (牌堆位置)")]
    public RectTransform drawPileTransform;

    [Header("单张抽牌动画时间")]
    public float drawCardDuration = 0.4f;

    [Header("抽牌间隔")]
    public float drawInterval = 0.1f;

    [Header("是否玩家手牌")]
    public bool isPlayerHand = true;

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

        if (attackSystem != null)
        {
            attackSystem.OnPhaseChanged += HandleAttackPhaseChanged;
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

        if (attackSystem != null)
        {
            attackSystem.OnPhaseChanged -= HandleAttackPhaseChanged;
        }
    }

    /// <summary>
    /// 抽多张牌
    /// </summary>
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

    /// <summary>
    /// 抽一张牌
    /// </summary>
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

        // 抽牌后检查：如果没有空槽位了，自动切换到攻击模式
        CheckAndSwitchMode();
    }

    private void CreateCard(CardDataSO data)
    {
        if (cardPrefab == null || handController == null) return;

        GameObject cardObj = Instantiate(cardPrefab);
        CardView cardView = cardObj.GetComponent<CardView>();
        if (cardView == null)
            cardView = cardObj.AddComponent<CardView>();

        cardView.Init(data);
        cardView.isPlayerCard = isPlayerHand;

        if (isPlayerHand)
            cardView.Reveal();
        else
            cardView.Hide();

        // 确保有 CardDragHandler
        CardDragHandler dragHandler = cardObj.GetComponent<CardDragHandler>();
        if (dragHandler == null && isPlayerHand)
        {
            dragHandler = cardObj.AddComponent<CardDragHandler>();
        }

        if (dragHandler != null)
        {
            dragHandler.OnDragStarted += HandleCardDragStarted;
            dragHandler.OnDragEnded += HandleCardDragEnded;
            dragHandler.OnPlacedToSlot += HandleCardPlacedToSlot;
        }

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
        cardView.isPlayerCard = isPlayerHand;
        cardView.Hide();

        CardDragHandler dragHandler = cardObj.GetComponent<CardDragHandler>();
        if (dragHandler == null && isPlayerHand)
            dragHandler = cardObj.AddComponent<CardDragHandler>();

        if (dragHandler != null)
        {
            dragHandler.OnDragStarted += HandleCardDragStarted;
            dragHandler.OnDragEnded += HandleCardDragEnded;
            dragHandler.OnPlacedToSlot += HandleCardPlacedToSlot;
        }

        rect.SetParent(handController.transform, false);
        rect.position = drawPileTransform.position;
        rect.localScale = Vector3.zero;

        cardViews.Add(cardView);
        handController.AddCard(cardObj, false);

        float t = 0f;
        if (isPlayerHand)
            cardView.Reveal();
        else
            cardView.Hide();

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

        CheckAndSwitchMode();
    }

    public void RemoveCard(CardView card)
    {
        if (card == null) return;

        CardDragHandler dragHandler = card.GetComponent<CardDragHandler>();
        if (dragHandler != null)
        {
            dragHandler.OnDragStarted -= HandleCardDragStarted;
            dragHandler.OnDragEnded -= HandleCardDragEnded;
            dragHandler.OnPlacedToSlot -= HandleCardPlacedToSlot;
        }

        cardViews.Remove(card);
        handController.RemoveCard(card.gameObject);
    }

    // 拖拽开始：暂停手牌布局
    private void HandleCardDragStarted(CardDragHandler handler)
    {
        if (handController != null)
            handController.PauseLayout();
    }

    // 拖拽结束：恢复布局
    private void HandleCardDragEnded(CardDragHandler handler)
    {
        if (handController != null)
            handController.ResumeLayout(true);
    }

    // 卡牌被放置到槽位
    private void HandleCardPlacedToSlot(CardDragHandler handler, SlotView slot)
    {
        if (handler == null || handler.CardView == null) return;

        CardView card = handler.CardView;

        if (cardViews.Contains(card))
        {
            cardViews.Remove(card);
            handController.RemoveCard(card.gameObject, false);
        }

        OnCardPlacedToSlot?.Invoke(card, slot.slotIndex);

        // 放置后检查模式切换
        CheckAndSwitchMode();
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
        if (selectedCard == card)
        {
            selectedCard.SetSelected(false);
            selectedCard = null;
            if (battleField != null)
                battleField.SetSelectedCard(null);
        }
    }

    private void HandleAttackPhaseChanged(AttackSystem.AttackPhase phase)
    {
        // 攻击模式下，手牌不可交互（可选
    }

    /// <summary>
    /// 检查并切换模式（有空槽位→放置模式；没空槽位→攻击模式
    /// </summary>
    private void CheckAndSwitchMode()
    {
        if (battleField == null) return;

        bool hasEmptySlot = battleField.HasEmptyPlayerSlots();

        if (hasEmptySlot)
        {
            // 有空槽位：放置模式
            if (!battleField.isPlacementMode)
                battleField.SetMode(true);
        }
        else
        {
            // 没空槽位：攻击模式 + 提示
            if (battleField.isPlacementMode)
                battleField.SetMode(false);
        }
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
