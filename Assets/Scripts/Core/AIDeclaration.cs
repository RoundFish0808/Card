using System;
using System.Collections.Generic;

/// <summary>
/// AI宣言数据
/// 根据架构文档 §3.3.1
/// </summary>
[Serializable]
public struct AIDeclaration
{
    /// <summary>位置提示文本（如"左边最强"、"中间最强"）</summary>
    public string PositionHint;

    /// <summary>数值提示文本（如"有一张A"）</summary>
    public string ValueHint;

    /// <summary>信息量等级（0~3），难度越高此值越小，透露信息越少</summary>
    public int InfoLevel;

    /// <summary>该宣言是否包含位置提示</summary>
    public bool HasPositionHint => !string.IsNullOrEmpty(PositionHint);

    /// <summary>该宣言是否包含数值提示</summary>
    public bool HasValueHint => !string.IsNullOrEmpty(ValueHint);

    public static readonly AIDeclaration Empty = new AIDeclaration
    {
        PositionHint = "",
        ValueHint = "",
        InfoLevel = 0
    };

    public override string ToString()
    {
        if (HasPositionHint && HasValueHint)
            return $"{PositionHint}，{ValueHint}";
        if (HasPositionHint)
            return PositionHint;
        if (HasValueHint)
            return ValueHint;
        return "……";
    }
}
