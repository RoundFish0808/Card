using UnityEngine;
using System.Collections.Generic;

public class RandomAIStrategy : IAIStrategy
{
    public List<CardDataSO> Arrange(List<CardDataSO> hand)
    {
        if (hand == null || hand.Count == 0)
            return new List<CardDataSO>();

        // 简单随机打乱顺序
        var shuffled = new List<CardDataSO>(hand);
        int n = shuffled.Count;
        while (n > 1)
        {
            n--;
            int k = Random.Range(0, n + 1);
            var temp = shuffled[k];
            shuffled[k] = shuffled[n];
            shuffled[n] = temp;
        }

        // 不足3张时补齐（理论上不应该发生）
        while (shuffled.Count < 3)
            shuffled.Add(shuffled[Random.Range(0, shuffled.Count)]);

        return shuffled.GetRange(0, Mathf.Min(3, shuffled.Count));
    }
}
