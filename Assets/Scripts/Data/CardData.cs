using UnityEngine;

[System.Serializable]
public class CardData
{
    public int id;        // 唯一编号，0-51
    public Suit suit;     // 花色
    public Rank rank;     // 点数
    public Sprite sprite; // 牌图，初期可以为空

    public CardData(int id, Suit suit, Rank rank, Sprite sprite = null)
    {
        this.id = id;
        this.suit = suit;
        this.rank = rank;
        this.sprite = sprite;
    }

    public string GetName()
    {
        return GetSuitName() + GetRankName();
    }

    private string GetSuitName()
    {
        switch (suit)
        {
            case Suit.Spade:
                return "黑桃";
            case Suit.Heart:
                return "红桃";
            case Suit.Diamond:
                return "方块";
            case Suit.Club:
                return "梅花";
            default:
                return "";
        }
    }

    private string GetRankName()
    {
        switch (rank)
        {
            case Rank.Jack:
                return "J";
            case Rank.Queen:
                return "Q";
            case Rank.King:
                return "K";
            case Rank.Ace:
                return "A";
            default:
                return ((int)rank).ToString();
        }
    }

    public override string ToString()
    {
        return GetName();
    }
}