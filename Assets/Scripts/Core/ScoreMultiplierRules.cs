using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 小局牌型倍率配置。
/// 这里保持静态，方便后续直接改数值或替换为 ScriptableObject 配置。
/// </summary>
public static class ScoreMultiplierRules
{
    public const float None = 1f;
    public const float Pair = 1.5f;
    public const float Flush = 2.0f;
    public const float Straight = 2.5f;
    public const float ThreeOfKind = 5.0f;
    public const float StraightFlush = 10.0f;

    /// <summary>
    /// 根据玩家在一小局内实际用于攻击的 3 张牌计算倍率。
    /// 只有刚好 3 张牌时才结算牌型，否则不加成。
    /// </summary>
    public static float GetMultiplier(IReadOnlyList<CardDataSO> cards, out string comboName)
    {
        comboName = "无";

        if (cards == null || cards.Count != 3 || cards.Any(card => card == null))
            return None;

        bool isFlush = cards.All(card => card.suit == cards[0].suit);
        bool isStraight = IsStraight(cards);
        bool isThreeOfKind = cards.GroupBy(card => card.rank).Any(group => group.Count() == 3);
        bool isPair = cards.GroupBy(card => card.rank).Any(group => group.Count() == 2);

        if (isStraight && isFlush)
        {
            comboName = "同花顺";
            return StraightFlush;
        }

        if (isThreeOfKind)
        {
            comboName = "三条";
            return ThreeOfKind;
        }

        if (isStraight)
        {
            comboName = "顺子";
            return Straight;
        }

        if (isFlush)
        {
            comboName = "同花";
            return Flush;
        }

        if (isPair)
        {
            comboName = "对子";
            return Pair;
        }

        return None;
    }

    private static bool IsStraight(IReadOnlyList<CardDataSO> cards)
    {
        List<int> values = cards
            .Select(card => GetStraightValue(card.rank))
            .OrderBy(value => value)
            .ToList();

        return values[1] == values[0] + 1 && values[2] == values[1] + 1;
    }

    private static int GetStraightValue(Rank rank)
    {
        // A 在顺子判断中按 14 处理，让 Q-K-A 可以成立。
        return rank == Rank.Ace ? 14 : (int)rank;
    }
}
