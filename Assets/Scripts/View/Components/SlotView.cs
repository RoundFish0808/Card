using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening;
using System;

public class SlotView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("槽位索引")]
    public int slotIndex;

    [Header("槽位归属")]
    public bool isPlayerSlot = true;

    [Header("背景图片")]
    public Image slotBackground;

    [Header("高亮边框")]
    public GameObject highlightFrame;

    [Header("槽位颜色")]
    public Color normalColor = new Color(0.2f, 0.2f, 0.2f, 0.5f);
    public Color highlightedColor = new Color(0.8f, 0.8f, 0.3f, 0.6f);
    public Color occupiedColor = new Color(0.3f, 0.3f, 0.3f, 0.6f);
    public Color attackTargetColor = new Color(1f, 0.3f, 0.3f, 0.7f);

    [Header("放置动画")]
    public float placeDuration = 0.3f;
    public Ease placeEase = Ease.OutBack;

    [Header("是否可交互")]
    public bool interactable = true;

    [Header("是否可作为攻击目标")]
    public bool canBeAttackTarget = false;

    public bool IsOccupied => CurrentCard != null;
    public CardView CurrentCard { get; private set; }

    public event Action<SlotView> OnSlotClicked;
    public event Action<SlotView> OnSlotEnter;
    public event Action<SlotView> OnSlotExit;
    public event Action<SlotView, CardView> OnCardPlaced;

    private RectTransform rectTransform;
    private bool isAttackTargetHighlighted = false;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        if (slotBackground == null)
            slotBackground = GetComponent<Image>();

        SetNormalState();
    }

    /// <summary>
    /// 放置卡牌到槽位（卡牌 RectTransform 与槽位完全一致：位置、大小、旋转、缩放
    /// </summary>
    public void PlaceCard(CardView card, bool withAnimation = false)
    {
        if (card == null) return;


        Debug.Log($"PlaceCard {card.name} to Slot {slotIndex}");


        CurrentCard = card;
        RectTransform cardRect = card.GetComponent<RectTransform>();
        if (rectTransform == null) rectTransform = GetComponent<RectTransform>();

        // 优先用名为 "CardSlot" 的子物体作为容器（不受 Layout Group 影响，anchors 稳定）
        RectTransform parentRT = rectTransform;
        var slotChild = transform.Find("CardSlot");
        if (slotChild != null)
            parentRT = slotChild.GetComponent<RectTransform>();

        DOTween.Kill(cardRect, false);

        // 第一步：先记录当前世界位置，然后把卡牌挂到目标容器下
        Vector3 startWorldPos = cardRect.position;

        card.transform.SetParent(parentRT, false);
        card.transform.SetAsLastSibling();

        // 第二步：卡牌 anchors 设为 (0.5, 0.5)，pivot 设为 (0.5, 0.5)
        cardRect.anchorMin = new Vector2(0.5f, 0.5f);
        cardRect.anchorMax = new Vector2(0.5f, 0.5f);
        cardRect.pivot = new Vector2(0.5f, 0.5f);
        
        Vector3 pos = cardRect.position;
        pos.x = 0;
        pos.y = 0;
        cardRect.position = pos;

        // 第三步：计算目标缩放（等比缩放到槽位尺寸）
        float baseScale = ComputeFitScale(cardRect, parentRT);
        Vector3 targetScale = new Vector3(baseScale * card.placedBaseScale, baseScale * card.placedBaseScale, 1f);

        Debug.Log($"当前卡牌为{card}的rect transform: {cardRect.position.x} {cardRect.position.y}");

        if (withAnimation)
        {
            // 把 card 设回起始世界位置（视觉上保持不跳变）
            cardRect.position = startWorldPos;
            Vector2 startAnchoredPos = cardRect.anchoredPosition;

            // 用 DOAnchorPos 动画到 (0, 0)
            cardRect.DOAnchorPos(Vector2.zero, placeDuration)
                .From(startAnchoredPos)
                .SetEase(placeEase);

            cardRect.DOScale(targetScale, placeDuration)
                .SetEase(placeEase);

            cardRect.DOLocalRotate(Vector3.zero, placeDuration)
                .SetEase(placeEase)
                .OnComplete(() =>
                {
                    card.SetState(CardState.Placed);
                    // 动画结束后强制清零，确保最终状态完全正确
                    cardRect.anchoredPosition = Vector2.zero;
                    cardRect.localRotation = Quaternion.identity;
                    cardRect.localScale = targetScale;
                });
        }
        else
        {
            cardRect.anchoredPosition = Vector2.zero;
            cardRect.localScale = targetScale;
            cardRect.localRotation = Quaternion.identity;
            card.SetState(CardState.Placed);
        }

        SetOccupiedState();
        OnCardPlaced?.Invoke(this, card);
    }

    /// <summary>
    /// 计算让卡牌填满槽位所需的等比缩放
    /// </summary>
    private float ComputeFitScale(RectTransform cardRect, RectTransform parentRT)
    {
        Vector2 slotSize = parentRT.rect.size;
        Vector2 cardSize = cardRect.rect.size;
        if (cardSize.x <= 0 || cardSize.y <= 0) return 1f;
        return Mathf.Min(slotSize.x / cardSize.x, slotSize.y / cardSize.y);
    }

    /// <summary>
    /// 从槽位移除卡牌
    /// </summary>
    public CardView RemoveCard()
    {
        CardView card = CurrentCard;
        if (card != null)
        {
            card.SetState(CardState.InHand);
            RectTransform cardRect = card.GetComponent<RectTransform>();
            // 恢复中心锚点 + 0 偏移 + 0 旋转
            cardRect.anchorMin = new Vector2(0.5f, 0.5f);
            cardRect.anchorMax = new Vector2(0.5f, 0.5f);
            cardRect.pivot = new Vector2(0.5f, 0.5f);
            cardRect.anchoredPosition = Vector2.zero;
            cardRect.localRotation = Quaternion.identity;
            cardRect.localScale = Vector3.one;
        }
        CurrentCard = null;
        SetNormalState();
        return card;
    }

    /// <summary>
    /// 设置高亮（悬停/可放置时用）
    /// </summary>
    public void SetHighlighted(bool highlighted)
    {
        if (!interactable || IsOccupied) return;
        if (isAttackTargetHighlighted) return;

        if (highlightFrame != null)
            highlightFrame.SetActive(highlighted);

        if (slotBackground != null)
            slotBackground.color = highlighted ? highlightedColor : normalColor;
    }

    /// <summary>
    /// 设置为攻击目标高亮状态（红色）
    /// </summary>
    public void SetAttackTargetHighlight(bool highlighted)
    {
        isAttackTargetHighlighted = highlighted;

        if (highlightFrame != null)
            highlightFrame.SetActive(highlighted);

        if (slotBackground != null)
        {
            if (highlighted)
                slotBackground.color = attackTargetColor;
            else if (IsOccupied)
                slotBackground.color = occupiedColor;
            else
                slotBackground.color = normalColor;
        }
    }

    public void SetInteractable(bool value)
    {
        interactable = value;
        if (!interactable)
        {
            SetHighlighted(false);
            SetAttackTargetHighlight(false);
        }
    }

    private void SetNormalState()
    {
        if (slotBackground != null)
            slotBackground.color = normalColor;

        if (highlightFrame != null)
            highlightFrame.SetActive(false);
    }

    private void SetOccupiedState()
    {
        if (slotBackground != null)
            slotBackground.color = occupiedColor;

        if (highlightFrame != null)
            highlightFrame.SetActive(false);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!interactable) return;
        OnSlotEnter?.Invoke(this);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!interactable) return;
        OnSlotExit?.Invoke(this);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!interactable) return;
        OnSlotClicked?.Invoke(this);
    }
}
