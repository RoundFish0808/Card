using UnityEngine;

/// <summary>
/// �ƵĻ�ɫ
/// </summary>
public enum Suit
{
    Spade,    // ����
    Heart,    // ����
    Diamond,  // ����
    Club      // ÷��
}

/// <summary>
/// �Ƶĵ���
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
/// �������ͣ������� / ��ͷ�� / A
/// </summary>
public enum CardType
{
    Number, // 2-10
    Face,   // J/Q/K
    Ace     // A
}

/// <summary>
/// ս�����
/// </summary>
public enum BattleResult
{
    Win,
    Lose,
    Draw
}

/// <summary>
/// 花色特效枚举
/// </summary>
public enum SuitEffectType
{
    None,

    // 黑桃：胜时差值翻倍
    WinDifferenceMultiplier,

    // 红桃：败时基本分+1(只有一次)
    LoseBaseBonus,

    // 方块：抽1弃1
    DrawAndDiscard,

    // 梅花：败时差值分拿一半 (取整)
    LoseDifferenceBonus
}

/// <summary>
/// AI难度等级
/// </summary>
public enum AIDifficulty
{
    Easy,       // 信息量等级 3（透露最多）
    Normal,     // 信息量等级 2
    Hard,       // 信息量等级 1
    Nightmare   // 信息量等级 0（几乎不透露）
}