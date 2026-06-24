using UnityEngine;

/// <summary>
/// 牌的花色
/// </summary>
public enum Suit
{
    Spade,    // 黑桃
    Heart,    // 红桃
    Diamond,  // 方块
    Club      // 梅花
}

/// <summary>
/// 牌的点数
/// </summary>
public enum Rank
{
    Two = 2,
    Three = 3,
    Four = 4,
    Five = 5,
    Six = 6,
    Seven = 7,
    Eight = 8,
    Nine = 9,
    Ten = 10,
    Jack = 11,
    Queen = 12,
    King = 13,
    Ace = 15
}

/// <summary>
/// 卡牌类型：数字牌 / 人头牌 / A
/// </summary>
public enum CardType
{
    Number, // 2-10
    Face,   // J/Q/K
    Ace     // A
}

/// <summary>
/// 战斗结果
/// </summary>
public enum BattleResult
{
    Win,
    Lose,
    Draw
}

/// <summary>
/// 花色效果枚举
/// </summary>
public enum SuitEffectType
{
    None,

    // 黑桃：胜利时差值翻倍
    WinDifferenceMultiplier,

    // 红桃：失败时基础分 +1(只加一分)
    LoseBaseBonus,

    // 方块：抽1弃1
    DrawAndDiscard,

    // 梅花：失败时获得一半差值 (向上取整四舍五入)
    LoseDifferenceBonus
}