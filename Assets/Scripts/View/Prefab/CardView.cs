using UnityEngine;
using UnityEngine.UI;

public class CardView : MonoBehaviour
{
    [Header("牌面图片")]
    public Image cardImage;

    private CardDataSO cardData;

    public void Init(CardDataSO data)
    {
        cardData = data;

        if (cardImage == null)
        {
            Debug.LogError("CardView 没有绑定 cardImage");
            return;
        }

        if (cardData == null)
        {
            Debug.LogError("传入的 CardDataSO 是空的");
            return;
        }

        cardImage.sprite = cardData.cardSprite;
    }

    public CardDataSO GetCardData()
    {
        return cardData;
    }
}