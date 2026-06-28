using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;

/// <summary>
/// NPC / AI 的展示层控制器
/// 根据架构文档 §3.3，与 AIController + AIDeclarer 配合：
/// - 管理 NPC 手牌的可视化（弧形排列、牌背显示）
/// - AI 布阵后生成宣言并通过 DeclarationView 展示
/// - 接收出牌指令后将卡牌放置到战场槽位
/// </summary>
public class NPCPresenter : MonoBehaviour
{
    [Header("NPC手牌控制器 (弧形朝上)")]
    public HandController npcHandController;

    [Header("战场视图")]
    public BattleFieldView battleField;

    [Header("宣言视图")]
    public DeclarationView declarationView;

    [Header("卡牌预制体")]
    public GameObject cardPrefab;

    [Header("宣言配置")]
    public AIDeclarationConfig declarationConfig;

    [Header("AI难度（决定信息量等级）")]
    public AIDifficulty difficulty = AIDifficulty.Normal;

    [Header("出牌动画时间")]
    public float playCardDuration = 0.5f;

    [Header("默认显示正面 (调试用)")]
    public bool debugShowFront = false;

    private IAIStrategy aiStrategy;
    private readonly List<CardDataSO> npcHandData = new List<CardDataSO>();
    private readonly List<CardView> npcHandViews = new List<CardView>();

    /// <summary>
    /// AI布阵后的手牌顺序（索引0=左边，1=中间，2=右边）
    /// 在 GenerateAndShowDeclaration() 后填充
    /// </summary>
    private List<CardDataSO> arrangedCards = new List<CardDataSO>();

    private void Awake()
    {
        aiStrategy = new RandomAIStrategy();
    }

    public void SetStrategy(IAIStrategy strategy)
    {
        aiStrategy = strategy;
    }

    /// <summary>
    /// 初始化 NPC 手牌（仅设置手牌，不生成宣言）
    /// </summary>
    public void InitializeHand(List<CardDataSO> initialCards)
    {
        if (initialCards == null) return;

        npcHandData.Clear();
        npcHandViews.Clear();
        arrangedCards.Clear();

        if (npcHandController != null)
            npcHandController.ClearCards();

        foreach (var data in initialCards)
        {
            AddCardToHand(data);
        }

        if (npcHandController != null)
            npcHandController.RefreshLayout();
    }

    /// <summary>
    /// 添加一张牌到手牌（仅视图层，不修改数据层）
    /// </summary>
    public void AddCardToHand(CardDataSO data)
    {
        if (data == null || cardPrefab == null) return;

        npcHandData.Add(data);

        GameObject cardObj = Instantiate(cardPrefab);
        CardView cardView = cardObj.GetComponent<CardView>();
        if (cardView == null)
            cardView = cardObj.AddComponent<CardView>();

        cardView.Init(data);

        if (debugShowFront)
            cardView.Reveal();
        else
            cardView.Hide();

        npcHandViews.Add(cardView);
        if (npcHandController != null)
            npcHandController.AddCard(cardObj, false);
    }

    /// <summary>
    /// AI 布阵 + 生成宣言 + 显示气泡
    /// 完整流程：策略布阵 → 分析最强牌 → 根据难度生成宣言 → DeclarationView 显示
    /// </summary>
    public void GenerateAndShowDeclaration()
    {
        if (npcHandData.Count < 3)
        {
            Debug.LogWarning("NPCPresenter: 手牌不足3张，无法生成宣言");
            return;
        }

        // Step 1: AI 布阵（使用策略决定左→中→右的顺序）
        arrangedCards = aiStrategy.Arrange(new List<CardDataSO>(npcHandData));

        // Step 2: 根据难度获取信息量等级
        int infoLevel = AIDeclarer.GetInfoLevelByDifficulty(difficulty);

        // Step 3: 生成宣言（分析最强牌位置 + 根据 InfoLevel 决定透露多少）
        var declaration = AIDeclarer.Generate(declarationConfig, arrangedCards, infoLevel);

        // Step 4: 通过 DeclarationView 显示
        if (declarationView != null)
        {
            string displayText = declaration.ToString();
            if (!string.IsNullOrEmpty(displayText))
                declarationView.ShowDeclaration(displayText);
        }

        Debug.Log($"[NPC] 宣言生成 (难度={difficulty}, InfoLevel={infoLevel}): {declaration.PositionHint} | {declaration.ValueHint}");
    }

    /// <summary>
    /// 获取布阵后的手牌（用于 AI 出牌到槽位）
    /// </summary>
    public List<CardDataSO> GetArrangedCards()
    {
        return new List<CardDataSO>(arrangedCards);
    }

    /// <summary>
    /// 将布阵中指定索引的卡牌放置到 NPC 槽位
    /// </summary>
    /// <param name="arrangedIndex">布阵中的索引（0=左, 1=中, 2=右）</param>
    /// <param name="slotIndex">槽位索引（0~2）</param>
    public void PlaceArrangedCard(int arrangedIndex, int slotIndex)
    {
        if (arrangedIndex < 0 || arrangedIndex >= arrangedCards.Count)
        {
            Debug.LogWarning($"NPCPresenter: 无效的布阵索引 {arrangedIndex}");
            return;
        }

        CardDataSO cardData = arrangedCards[arrangedIndex];

        // 找到对应的 CardView
        int viewIndex = npcHandData.IndexOf(cardData);
        if (viewIndex < 0 || viewIndex >= npcHandViews.Count)
        {
            Debug.LogWarning("NPCPresenter: 找不到对应的 CardView");
            return;
        }

        CardView card = npcHandViews[viewIndex];

        // 从手牌移除
        npcHandViews.RemoveAt(viewIndex);
        npcHandData.RemoveAt(viewIndex);
        arrangedCards.RemoveAt(arrangedIndex);

        if (npcHandController != null)
            npcHandController.RemoveCard(card.gameObject);

        // 翻牌并放置到槽位
        card.Reveal();
        battleField?.PlaceCardToNPCSlot(slotIndex, card, true);

        // 刷新 NPC 手牌弧形布局
        if (npcHandController != null)
            npcHandController.RefreshLayout();
    }

    /// <summary>
    /// 展示战斗结果时的宣言（胜/负/平）
    /// </summary>
    public void ShowResultDeclaration(BattleResult result)
    {
        if (declarationView == null || declarationConfig == null) return;

        // 根据战斗结果显示不同台词（这部分可以后续扩展）
        string resultText = result switch
        {
            BattleResult.Win => "哼，不过如此。",
            BattleResult.Lose => "居然输了……",
            BattleResult.Draw => "势均力敌啊。",
            _ => ""
        };

        if (!string.IsNullOrEmpty(resultText))
            declarationView.ShowDeclaration(resultText);
    }

    /// <summary>
    /// 展示所有卡牌正面（用于小局结算时）
    /// </summary>
    public void RevealAllCards()
    {
        foreach (var card in npcHandViews)
            card.Reveal();
    }

    /// <summary>
    /// 隐藏所有卡牌（显示牌背）
    /// </summary>
    public void HideAllCards()
    {
        foreach (var card in npcHandViews)
            card.Hide();
    }

    public int HandCardCount => npcHandData.Count;
    public int ArrangedCardCount => arrangedCards.Count;
}
