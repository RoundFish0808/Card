using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// AI宣言生成器
/// 根据架构文档 §3.3 — 基于布阵结果和难度等级生成有策略意义的宣言
/// </summary>
public static class AIDeclarer
{
    /// <summary>
    /// 根据布阵结果和难度等级生成宣言
    /// </summary>
    /// <param name="config">宣言配置SO</param>
    /// <param name="arrangedCards">布阵后的3张牌（顺序: 左→中→右）</param>
    /// <param name="infoLevel">信息量等级（0~3）</param>
    /// <returns>完整的AIDeclaration</returns>
    public static AIDeclaration Generate(AIDeclarationConfig config, List<CardDataSO> arrangedCards, int infoLevel)
    {
        if (config == null || arrangedCards == null || arrangedCards.Count < 3)
        {
            return AIDeclaration.Empty;
        }

        var declaration = new AIDeclaration
        {
            InfoLevel = infoLevel
        };

        // 分析布阵：找出最强牌的位置
        int strongestIndex = FindStrongestCardIndex(arrangedCards);
        string strongestPosition = GetPositionName(strongestIndex);
        string strongestCardName = GetCardName(arrangedCards[strongestIndex]);
        string rankDescription = GetRankDescription(arrangedCards[strongestIndex]);

        // 根据信息量等级决定透露多少
        switch (infoLevel)
        {
            case 3: // Easy — 位置 + 数值都透露
                declaration.PositionHint = config.GetPositionDeclaration(strongestPosition);
                declaration.ValueHint = config.GetValueDeclaration(strongestCardName);
                break;

            case 2: // Normal — 随机透露位置或数值之一
                if (Random.value < 0.5f)
                    declaration.PositionHint = config.GetPositionDeclaration(strongestPosition);
                else
                    declaration.ValueHint = config.GetValueDeclaration(strongestCardName);
                break;

            case 1: // Hard — 模糊信息
                declaration.PositionHint = config.GetVagueDeclaration();
                declaration.ValueHint = config.GetVagueDeclaration();
                break;

            case 0: // Nightmare — 几乎不透露
            default:
                declaration.PositionHint = config.GetMinimalDeclaration();
                declaration.ValueHint = "";
                break;
        }

        return declaration;
    }

    /// <summary>
    /// 根据难度获取信息量等级
    /// </summary>
    public static int GetInfoLevelByDifficulty(AIDifficulty difficulty)
    {
        return difficulty switch
        {
            AIDifficulty.Easy => 3,
            AIDifficulty.Normal => 2,
            AIDifficulty.Hard => 1,
            AIDifficulty.Nightmare => 0,
            _ => 2
        };
    }

    /// <summary>
    /// 在3张牌中找出最强牌的索引（0=左, 1=中, 2=右）
    /// </summary>
    private static int FindStrongestCardIndex(List<CardDataSO> cards)
    {
        int bestIndex = 0;
        int bestValue = cards[0].GetBattleValue();

        for (int i = 1; i < cards.Count; i++)
        {
            int value = cards[i].GetBattleValue();
            if (value > bestValue)
            {
                bestValue = value;
                bestIndex = i;
            }
        }

        return bestIndex;
    }

    /// <summary>
    /// 获取位置名称
    /// </summary>
    private static string GetPositionName(int index)
    {
        return index switch
        {
            0 => "左边",
            1 => "中间",
            2 => "右边",
            _ => ""
        };
    }

    /// <summary>
    /// 获取卡牌显示名称
    /// </summary>
    private static string GetCardName(CardDataSO card)
    {
        if (card == null) return "未知";

        string suitName = card.suit switch
        {
            Suit.Spade => "黑桃",
            Suit.Heart => "红桃",
            Suit.Diamond => "方块",
            Suit.Club => "梅花",
            _ => ""
        };

        string rankName = card.rank switch
        {
            Rank.Ace => "A",
            Rank.Jack => "J",
            Rank.Queen => "Q",
            Rank.King => "K",
            _ => ((int)card.rank).ToString()
        };

        return $"{suitName}{rankName}";
    }

    /// <summary>
    /// 获取牌面描述（人头牌统一叫"人头牌"）
    /// </summary>
    private static string GetRankDescription(CardDataSO card)
    {
        if (card == null) return "未知";

        return card.rank switch
        {
            Rank.Ace => "A",
            Rank.Jack or Rank.Queen or Rank.King => "人头牌",
            _ => ((int)card.rank).ToString()
        };
    }
}
