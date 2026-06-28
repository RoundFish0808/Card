using System.Collections.Generic;

public interface IAIStrategy
{
    /// <summary>
    /// 输入3张手牌，输出布阵后的顺序（左→中→右）
    /// </summary>
    List<CardDataSO> Arrange(List<CardDataSO> hand);
}
