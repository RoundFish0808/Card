using UnityEngine;

/// <summary>
/// 默认战斗计算器。
/// 规则：双方都有 2 点保底分；点数高的一方额外获得差值分；花色效果在此基础上修正。
/// </summary>
public class StandardBattleCalculator : IBattleCalculator
{
    private const int BaseScore = 2;

    public BattleResultData Calculate(CardDataSO attacker, CardDataSO defender)
    {
        if (attacker == null || defender == null)
            return BattleResultData.Empty;

        int playerValue = attacker.GetBattleValue();
        int npcValue = defender.GetBattleValue();
        int diff = Mathf.Abs(playerValue - npcValue);

        BattleResult result = BattleResult.Draw;
        if (playerValue > npcValue)
            result = BattleResult.Win;
        else if (playerValue < npcValue)
            result = BattleResult.Lose;

        int playerScore = BaseScore;
        int npcScore = BaseScore;
        SuitEffectType triggeredEffect = SuitEffectType.None;

        if (result == BattleResult.Win)
        {
            int winDiff = ApplyWinSuitEffect(attacker, diff, ref triggeredEffect);
            playerScore += winDiff;
            npcScore += ApplyLoseSuitEffect(defender, diff, ref triggeredEffect);
        }
        else if (result == BattleResult.Lose)
        {
            int winDiff = ApplyWinSuitEffect(defender, diff, ref triggeredEffect);
            npcScore += winDiff;
            playerScore += ApplyLoseSuitEffect(attacker, diff, ref triggeredEffect);
        }

        return new BattleResultData
        {
            result = result,
            baseScore = BaseScore,
            diffScore = diff,
            totalScore = playerScore,
            playerScore = playerScore,
            npcScore = npcScore,
            playerBattleValue = playerValue,
            npcBattleValue = npcValue,
            triggeredEffect = triggeredEffect
        };
    }

    private int ApplyWinSuitEffect(CardDataSO winningCard, int diff, ref SuitEffectType triggeredEffect)
    {
        if (winningCard.suit == Suit.Spade)
        {
            triggeredEffect = SuitEffectType.WinDifferenceMultiplier;
            return diff * 2;
        }

        return diff;
    }

    private int ApplyLoseSuitEffect(CardDataSO losingCard, int diff, ref SuitEffectType triggeredEffect)
    {
        switch (losingCard.suit)
        {
            case Suit.Heart:
                triggeredEffect = SuitEffectType.LoseBaseBonus;
                return 1;

            case Suit.Club:
                triggeredEffect = SuitEffectType.LoseDifferenceBonus;
                return Mathf.FloorToInt(diff * 0.5f);

            case Suit.Diamond:
                // 方块的“抽1弃1”属于手牌操作，先只标记效果，后续可在回合流程里接入弃牌逻辑。
                triggeredEffect = SuitEffectType.DrawAndDiscard;
                return 0;

            default:
                return 0;
        }
    }
}
