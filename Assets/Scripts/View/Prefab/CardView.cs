using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;

public class CardView : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("卡牌图片")]
    public Image cardImage;

    [Header("卡牌名称文本")]
    public Text cardNameText;

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

    private void Awake()
    {
        AutoFindReferences();
    }

    private void AutoFindReferences()
    {
        if (cardImage == null)
        {
            cardImage = GetComponentInChildren<Image>(true);
            if (cardImage != null && cardImage.gameObject == gameObject)
            {
                Image[] images = GetComponentsInChildren<Image>(true);
                foreach (var img in images)
                {
                    if (img.gameObject != gameObject)
                    {
                        cardImage = img;
                        break;
                    }
                }
            }
        }

        if (cardNameText == null)
        {
            Text[] texts = GetComponentsInChildren<Text>(true);
            if (texts.Length > 0)
                cardNameText = texts[0];
        }
    }

    public void Init(CardDataSO data)
    {
        cardData = data;

        if (cardImage == null)
        {
            Debug.LogWarning("CardView 找不到 Image 组件，请检查预制体结构");
            return;
        }

        if (cardData == null)
        {
            Debug.LogError("传入的 CardDataSO 是空的");
            return;
        }

        if (cardData.cardSprite != null)
        {
            cardImage.sprite = cardData.cardSprite;
        }

        if (cardNameText != null && !string.IsNullOrEmpty(cardData.cardName))
        {
            cardNameText.text = cardData.cardName;
        }

        UpdateVisual();
    }

    public void SetCardData(CardDataSO data)
    {
        cardData = data;
        if (cardImage != null && cardData != null && cardData.cardSprite != null)
            cardImage.sprite = cardData.cardSprite;

        if (cardNameText != null && cardData != null && !string.IsNullOrEmpty(cardData.cardName))
            cardNameText.text = cardData.cardName;
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

        if (cardNameText != null)
            cardNameText.enabled = isRevealed;
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
