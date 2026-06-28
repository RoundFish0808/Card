using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;

public class CardView : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("卡牌图片")]
    public Image cardImage;

    [Header("卡牌背面")]
    public GameObject cardBack;

    [Header("选中高亮")]
    public GameObject selectedHighlight;

    private CardDataSO cardData;
    private bool isRevealed = true;
    private bool isSelected = false;

    public event Action<CardView> OnClicked;
    public event Action<CardView> OnPointerEnterEvent;
    public event Action<CardView> OnPointerExitEvent;

    public CardDataSO CardData => cardData;
    public bool IsRevealed => isRevealed;
    public bool IsSelected => isSelected;

    public void Init(CardDataSO data)
    {
        cardData = data;

        if (cardImage == null)
        {
            Debug.LogError("CardView 没有挂 cardImage");
            return;
        }

        if (cardData == null)
        {
            Debug.LogError("传入的 CardDataSO 是空的");
            return;
        }

        cardImage.sprite = cardData.cardSprite;
        UpdateVisual();
    }

    public void SetCardData(CardDataSO data)
    {
        cardData = data;
        if (cardImage != null && cardData != null)
            cardImage.sprite = cardData.cardSprite;
    }

    public void Reveal()
    {
        isRevealed = true;
        UpdateVisual();
    }

    public void Hide()
    {
        isRevealed = false;
        UpdateVisual();
    }

    public void SetSelected(bool selected)
    {
        isSelected = selected;
        if (selectedHighlight != null)
            selectedHighlight.SetActive(selected);
    }

    private void UpdateVisual()
    {
        if (cardBack != null)
            cardBack.SetActive(!isRevealed);

        if (cardImage != null)
            cardImage.enabled = isRevealed;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        OnClicked?.Invoke(this);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        OnPointerEnterEvent?.Invoke(this);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        OnPointerExitEvent?.Invoke(this);
    }
}
