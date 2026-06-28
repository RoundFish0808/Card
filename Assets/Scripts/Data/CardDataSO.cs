using UnityEngine;

[CreateAssetMenu(fileName = "CardData", menuName = "Card/Card Data")]
public class CardDataSO : ScriptableObject
{
    [Header("基础信息")]
    public int id;
    public string cardName;

    [Header("卡牌属性")]
    public Suit suit;
    public Rank rank;

    [Header("牌面图片")]
    public Sprite cardSprite;

    public CardType GetCardType()
    {
        if (rank == Rank.Ace)
        {
            return CardType.Ace;
        }

        if (rank == Rank.Jack || rank == Rank.Queen || rank == Rank.King)
        {
            return CardType.Face;
        }

        return CardType.Number;
    }

    public int GetBattleValue()
    {
        return (int)rank;
    }
}