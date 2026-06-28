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

    [Header("放置动画")]
    public float placeDuration = 0.3f;
    public Ease placeEase = Ease.OutBack;

    [Header("是否可交互")]
    public bool interactable = true;

    public bool IsOccupied { get; private set; }
    public CardView CurrentCard { get; private set; }

    public event Action<SlotView> OnSlotClicked;
    public event Action<SlotView> OnSlotEnter;
    public event Action<SlotView> OnSlotExit;

    private RectTransform rectTransform;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        if (slotBackground == null)
            slotBackground = GetComponent<Image>();

        SetNormalState();
    }

    public void PlaceCard(CardView card, bool withAnimation = true)
    {
        if (card == null) return;

        IsOccupied = true;
        CurrentCard = card;

        RectTransform cardRect = card.GetComponent<RectTransform>();
        cardRect.SetParent(transform, false);

        if (withAnimation)
        {
            cardRect.DOAnchorPos(Vector2.zero, placeDuration)
                .SetEase(placeEase);

            cardRect.DOScale(Vector3.one, placeDuration)
                .SetEase(placeEase);

            Vector3 targetRot = isPlayerSlot ? Vector3.zero : new Vector3(0, 0, 180f);
            cardRect.DORotate(targetRot, placeDuration)
                .SetEase(placeEase);
        }
        else
        {
            cardRect.anchoredPosition = Vector2.zero;
            cardRect.localScale = Vector3.one;
            cardRect.localRotation = isPlayerSlot ? Quaternion.identity : Quaternion.Euler(0, 0, 180f);
        }

        SetOccupiedState();
    }

    public CardView RemoveCard()
    {
        CardView card = CurrentCard;
        CurrentCard = null;
        IsOccupied = false;
        SetNormalState();
        return card;
    }

    public void SetHighlighted(bool highlighted)
    {
        if (!interactable || IsOccupied) return;

        if (highlightFrame != null)
            highlightFrame.SetActive(highlighted);

        if (slotBackground != null)
            slotBackground.color = highlighted ? highlightedColor : normalColor;
    }

    public void SetInteractable(bool value)
    {
        interactable = value;
        if (!interactable)
            SetHighlighted(false);
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
        if (!interactable || IsOccupied) return;
        OnSlotClicked?.Invoke(this);
    }
}
