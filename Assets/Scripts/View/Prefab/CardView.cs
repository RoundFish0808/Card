using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;
using DG.Tweening;

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

    [Header("归属方（用于判断显示哪一面）")]
    public bool isPlayerCard = true;

    [Header("放置后的目标缩放（再次选中时切换）")]
    [Tooltip("放置后的基础缩放，再次选中时会被乘以选中缩放")]
    public float placedBaseScale = 1f;
    [Tooltip("再次选中已放置的卡牌时，缩放变为 placedBaseScale * placedSelectedScale")]
    public float placedSelectedScale = 1.1f;

    private CardDataSO cardData;
    private bool isRevealed = true;
    private bool isSelected = false;
    private CardState currentState = CardState.InHand;

    public event Action<CardView> OnClicked;
    public event Action<CardView> OnPointerEnterEvent;
    public event Action<CardView> OnPointerExitEvent;

    public event Action<CardView, CardState> OnStateChanged;

    public CardDataSO CardData => cardData;
    public bool IsRevealed => isRevealed;
    public bool IsSelected => isSelected;
    public CardState CurrentState => currentState;

    /// <summary>
    /// 当前应该使用的目标缩放（已放置：基础/选中态切换；手牌：1）
    /// </summary>
    public float PlacedScale
    {
        get
        {
            if (currentState == CardState.Placed)
                return isSelected ? placedBaseScale * placedSelectedScale : placedBaseScale;
            return 1f;
        }
    }

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

    /// <summary>
    /// 翻到正面
    /// </summary>
    public void Reveal()
    {
        isRevealed = true;
        UpdateVisual();
    }

    /// <summary>
    /// 翻到背面
    /// </summary>
    public void Hide()
    {
        isRevealed = false;
        UpdateVisual();
    }

    /// <summary>
    /// 设置选中状态
    /// 已放置的卡牌：选中时只调整 scale，不改变位置
    /// </summary>
    public void SetSelected(bool selected)
    {
        isSelected = selected;
        if (selectedHighlight != null)
            selectedHighlight.SetActive(selected);

        // 已放置的卡牌：选中时调整 scale（不放回手牌位置）
        if (currentState == CardState.Placed)
        {
            RectTransform cardRect = GetComponent<RectTransform>();
            if (cardRect != null)
            {
                DOTween.Kill(cardRect, false);
                float targetScale = PlacedScale;
                cardRect.DOScale(new Vector3(targetScale, targetScale, 1f), 0.15f).SetEase(Ease.OutBack);
            }
        }
    }

    /// <summary>
    /// 设置卡牌状态
    /// </summary>
    public void SetState(CardState newState)
    {
        if (currentState == newState) return;

        CardState oldState = currentState;
        currentState = newState;

        OnStateChanged?.Invoke(this, newState);

        UpdateVisual();
    }

    /// <summary>
    /// 获取战斗值（用于 AI 评估最强牌）
    /// </summary>
    public int GetBattleValue()
    {
        if (cardData == null) return 0;
        return cardData.GetBattleValue();
    }

    private void UpdateVisual()
    {
        bool showFront = isRevealed;

        // 放置在槽位中时：
        // - 玩家的牌：显示正面（玩家自己能看到）
        // - NPC的牌：显示背面（玩家看不到）
        if (currentState == CardState.Placed)
        {
            showFront = isPlayerCard;
        }

        if (cardBack != null)
            cardBack.SetActive(!showFront);

        if (cardImage != null)
            cardImage.enabled = showFront;

        if (cardNameText != null)
            cardNameText.enabled = showFront;
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
