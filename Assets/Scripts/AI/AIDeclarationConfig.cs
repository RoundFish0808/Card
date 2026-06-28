using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "AIDeclarationConfig", menuName = "Game/AI Declaration Config")]
public class AIDeclarationConfig : ScriptableObject
{
    [Header("--- 信息量等级说明 ---")]
    [Header("InfoLevel 3 = Easy（信息最多，位置+数值都透露）")]
    [Header("InfoLevel 2 = Normal（透露位置或数值之一）")]
    [Header("InfoLevel 1 = Hard（只透露模糊信息）")]
    [Header("InfoLevel 0 = Nightmare（几乎不透露）")]

    [Header("--- 位置宣言模板 ---")]
    [Tooltip("占位符 {0} 会被替换为最强位置（左边/中间/右边）")]
    public List<string> PositionTemplates = new List<string>
    {
        "左中右，{0}最强",
        "{0}这张牌，是我的王牌",
        "看好我的{0}！",
        "这局，{0}会给你惊喜"
    };

    [Header("--- 数值宣言模板 ---")]
    [Tooltip("占位符 {0} 会被替换为牌名（如A、人头牌）")]
    public List<string> ValueTemplates = new List<string>
    {
        "我手里有一张{0}",
        "这张{0}，你可要小心了",
        "别小看我的{0}",
        "我最好的牌是{0}"
    };

    [Header("--- 模糊宣言（低信息量时使用） ---")]
    [Tooltip("当 InfoLevel ≤ 1 时，随机使用这些模糊宣言")]
    public List<string> VagueTemplates = new List<string>
    {
        "……准备好了吗？",
        "让我看看你的实力。",
        "出招吧。",
        "这一局，我不会输。",
        "有点意思……"
    };

    [Header("--- 最小提示（Nightmare难度） ---")]
    public List<string> MinimalTemplates = new List<string>
    {
        "……",
        "哼。",
        "来吧。",
        "别大意。"
    };

    /// <summary>
    /// 根据信息等级获取一条位置宣言
    /// </summary>
    /// <param name="strongestPosition">最强牌的位置（Left/Middle/Right）</param>
    /// <returns>填充好的宣言文本</returns>
    public string GetPositionDeclaration(string strongestPosition)
    {
        if (PositionTemplates.Count == 0) return "";
        string template = PositionTemplates[Random.Range(0, PositionTemplates.Count)];
        return string.Format(template, strongestPosition);
    }

    /// <summary>
    /// 根据信息等级获取一条数值宣言
    /// </summary>
    /// <param name="cardName">最强牌的名称</param>
    /// <returns>填充好的宣言文本</returns>
    public string GetValueDeclaration(string cardName)
    {
        if (ValueTemplates.Count == 0) return "";
        string template = ValueTemplates[Random.Range(0, ValueTemplates.Count)];
        return string.Format(template, cardName);
    }

    /// <summary>
    /// 获取模糊宣言（低信息量时使用）
    /// </summary>
    public string GetVagueDeclaration()
    {
        if (VagueTemplates.Count == 0) return "……";
        return VagueTemplates[Random.Range(0, VagueTemplates.Count)];
    }

    /// <summary>
    /// 获取最小提示（Nightmare难度）
    /// </summary>
    public string GetMinimalDeclaration()
    {
        if (MinimalTemplates.Count == 0) return "……";
        return MinimalTemplates[Random.Range(0, MinimalTemplates.Count)];
    }
}
