/// <summary>
/// 战斗计算器接口
/// 预留接口，由另一个程序提供具体实现
/// 替换时只需实现此接口并赋值给 BattleFieldView / AttackSystem
/// </summary>
public interface IBattleCalculator
{
    /// <summary>
    /// 计算单轮战斗结果
    /// </summary>
    /// <param name="attacker">攻击方卡牌数据</param>
    /// <param name="defender">防守方卡牌数据</param>
    /// <returns>战斗结果（胜/负/平 + 得分）</returns>
    BattleResultData Calculate(CardDataSO attacker, CardDataSO defender);
}

/// <summary>
/// 战斗结果数据
/// </summary>
public struct BattleResultData
{
    public BattleResult result;       // 胜/负/平
    public int baseScore;             // 基本分
    public int diffScore;             // 差值分
    public int totalScore;            // 总分
    public int playerScore;           // 本回合玩家得分
    public int npcScore;              // 本回合 NPC 得分
    public int playerBattleValue;      // 玩家卡牌参与比较的点数
    public int npcBattleValue;         // NPC 卡牌参与比较的点数
    public SuitEffectType triggeredEffect; // 触发的花色特效

    public static readonly BattleResultData Empty = new BattleResultData
    {
        result = BattleResult.Draw,
        baseScore = 0,
        diffScore = 0,
        totalScore = 0,
        playerScore = 0,
        npcScore = 0,
        playerBattleValue = 0,
        npcBattleValue = 0,
        triggeredEffect = SuitEffectType.None
    };
}
