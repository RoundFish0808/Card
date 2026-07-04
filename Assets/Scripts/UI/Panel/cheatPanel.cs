using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class cheatPanel : BasePanel
{
    private const int CheatOptionCount = 5;

    private CardDataSO selectedCard;
    private CardView selectedCardView;
    private readonly List<CardDataSO> optionCards = new List<CardDataSO>();
    private readonly List<GameObject> optionCardObjects = new List<GameObject>();
    private bool hasConfirmed;

    public Button checkButton;
    public Transform[] cardPositions;

    public override void Init()
    {
        CreateCheatOptions();

        if (checkButton != null)
        {
            checkButton.onClick.RemoveListener(ConfirmSelectedCard);
            checkButton.onClick.AddListener(ConfirmSelectedCard);
        }
    }

    private void CreateCheatOptions()
    {
        if (CardManager.Instance == null)
        {
            Debug.LogWarning("[作弊面板] 找不到 CardManager，无法生成作弊牌");
            return;
        }

        if (cardPositions == null || cardPositions.Length < CheatOptionCount)
        {
            Debug.LogError("[作弊面板] 请在 Inspector 里拖入 5 个卡牌生成位置");
            UIManager.Instance.HidePanel<cheatPanel>(false);
            return;
        }

        optionCards.Clear();
        optionCards.AddRange(CardManager.Instance.StartCheatSelection(CheatOptionCount));

        if (optionCards.Count == 0)
        {
            UIManager.Instance.HidePanel<cheatPanel>(false);
            return;
        }

        for (int i = 0; i < optionCards.Count; i++)
        {
            Transform pos = i < cardPositions.Length ? cardPositions[i] : null;
            if (pos == null)
            {
                Debug.LogWarning("[作弊面板] 找不到 Pos" + (i + 1) + "，跳过该候选牌");
                continue;
            }

            GameObject cardObj = Instantiate(CardManager.Instance.cardPrefab, pos);
            optionCardObjects.Add(cardObj);

            RectTransform rect = cardObj.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.anchoredPosition = Vector2.zero;
                rect.localRotation = Quaternion.identity;
                rect.localScale = Vector3.one;
            }

            CardView cardView = cardObj.GetComponent<CardView>();
            if (cardView != null)
            {
                cardView.Init(optionCards[i]);
                cardView.isPlayerCard = true;
                cardView.Reveal();
                cardView.OnClicked += SelectCard;
            }

            CardDragHandler dragHandler = cardObj.GetComponent<CardDragHandler>();
            if (dragHandler != null)
                dragHandler.enabled = false;
        }
    }

    private void SelectCard(CardView cardView)
    {
        if (cardView == null) return;

        if (selectedCardView != null)
            selectedCardView.SetSelected(false);

        selectedCardView = cardView;
        selectedCard = cardView.CardData;
        selectedCardView.SetSelected(true);

        Debug.Log("[作弊面板] 已选择：" + selectedCard.cardName);
    }

    private void ConfirmSelectedCard()
    {
        if (selectedCard == null)
        {
            Debug.LogWarning("[作弊面板] 请先选择一张牌");
            return;
        }

        hasConfirmed = true;
        CardManager.Instance?.ConfirmCheatSelection(selectedCard, optionCards);
        UIManager.Instance.HidePanel<cheatPanel>();
    }

    private void OnDestroy()
    {
        foreach (GameObject cardObj in optionCardObjects)
        {
            if (cardObj == null) continue;

            CardView cardView = cardObj.GetComponent<CardView>();
            if (cardView != null)
                cardView.OnClicked -= SelectCard;
        }

        if (!hasConfirmed && optionCards.Count > 0)
            CardManager.Instance?.CancelCheatSelection(optionCards);
    }
}
