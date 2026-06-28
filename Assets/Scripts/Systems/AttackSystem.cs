using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;

/// <summary>
/// 攻击阶段
/// 攻击选择状态机
/// 流程：Idle → SelectAttacker（选择主攻牌) → SelectTarget（选择敌方目标） → Resolve（触发攻击
/// 预留 IBattleCalculator 接口由外部实现
/// </summary>
public class AttackSystem : MonoBehaviour
{
    public enum AttackPhase
    {
        Idle,               // 空闲（未进入攻击模式
        SelectingAttacker,    // 选择自己主攻卡牌
        SelectingTarget      // 选择敌方目标
    }

    [Header("战场视图")]
    public BattleFieldView battleField;

    [Header("战斗计算器（预留接口，由其他程序实现）")]
    public IBattleCalculator battleCalculator;

    [Header("攻击提示UI")]
    public GameObject attackHintPanel;   // "请选择攻击"提示面板
    public Text attackHintText;    // 提示文本

    [Header("提示文本")]
    public string selectAttackerHint = "请选择你的主攻卡牌";
    public string selectTargetHint = "请选择敌方目标";

    private AttackPhase currentPhase = AttackPhase.Idle;
    private CardView selectedAttacker;  // 选中的主攻牌
    private SlotView selectedAttackerSlot; // 主攻牌所在槽位

    public AttackPhase CurrentPhase => currentPhase;
    public CardView SelectedAttacker => selectedAttacker;
    public bool IsInAttackMode => currentPhase != AttackPhase.Idle;

    public event Action<CardView, CardView> OnAttackResolved;    // 攻击完成回调（主攻， 主攻牌， 目标牌）
    public event Action<AttackPhase> OnPhaseChanged;

    private void Awake()
    {
        SetPhase(AttackPhase.Idle);
    }

    /// <summary>
    /// 进入攻击模式
    /// </summary>
    public void EnterAttackMode()
    {
        if (battleField == null) return;

        SetPhase(AttackPhase.SelectingAttacker);
        ShowHint(selectAttackerHint);
        HighlightPlayerSlots(true);
    }

    /// <summary>
    /// 退出攻击模式
    /// </summary>
    public void ExitAttackMode()
    {
        SetPhase(AttackPhase.Idle);
        HideHint();
        ClearSelection();
        HighlightPlayerSlots(false);
        HighlightEnemySlots(false);
    }

    /// <summary>
    /// 当玩家点击了己方槽位（选择主攻牌）
    /// </summary>
    public void OnPlayerSlotClicked(SlotView slot)
    {
        if (currentPhase != AttackPhase.SelectingAttacker) return;
        if (!slot.IsOccupied) return;

        // 选中主攻牌
        selectedAttacker = slot.CurrentCard;
        selectedAttackerSlot = slot;
        selectedAttacker.SetSelected(true);

        SetPhase(AttackPhase.SelectingTarget);
        ShowHint(selectTargetHint);

        HighlightPlayerSlots(false);
        HighlightEnemySlots(true);
    }

    /// <summary>
    /// 当玩家点击了敌方槽位（选择攻击目标）
    /// </summary>
    public void OnEnemySlotClicked(SlotView slot)
    {
        if (currentPhase != AttackPhase.SelectingTarget) return;
        if (!slot.IsOccupied) return;

        CardView targetCard = slot.CurrentCard;

        // 触发攻击
        ResolveAttack(selectedAttacker, targetCard, slot);
    }

    /// <summary>
    /// 执行攻击结算
    /// </summary>
    private void ResolveAttack(CardView attacker, CardView target, SlotView targetSlot)
    {
        // 预留攻击计算（调用预留接口

        if (battleCalculator != null)
        {
            BattleResultData result = battleCalculator.Calculate(attacker.CardData, target.CardData);
            Debug.Log($"[AttackSystem] 攻击结算: {attacker.CardData.cardName} vs {target.CardData.cardName} → {result.result}, 得分: {result.totalScore}");
        }
        else
        {
            Debug.LogWarning("[AttackSystem] battleCalculator 未设置，跳过结算（预留接口");
        }

        OnAttackResolved?.Invoke(selectedAttacker, target);

        // 清理选择状态
        ClearSelection();
        ExitAttackMode();
    }

    private void SetPhase(AttackPhase phase)
    {
        currentPhase = phase;
        OnPhaseChanged?.Invoke(phase);
    }

    private void ClearSelection()
    {
        if (selectedAttacker != null)
        {
            selectedAttacker.SetSelected(false);
            selectedAttacker = null;
            selectedAttackerSlot = null;
        }
    }

    private void HighlightPlayerSlots(bool highlight)
    {
        if (battleField == null) return;

        foreach (var slot in battleField.playerSlots)
        {
            if (slot.IsOccupied)
            {
                if (highlight)
                    slot.SetAttackTargetHighlight(highlight);
                else
                    slot.SetAttackTargetHighlight(false);
            }
        }
    }

    private void HighlightEnemySlots(bool highlight)
    {
        if (battleField == null) return;

        foreach (var slot in battleField.npcSlots)
        {
            if (slot.IsOccupied)
            {
                slot.SetAttackTargetHighlight(highlight);
            }
        }
    }

    private void ShowHint(string text)
    {
        if (attackHintPanel != null)
            attackHintPanel.SetActive(true);

        if (attackHintText != null)
            attackHintText.text = text;
    }

    private void HideHint()
    {
        if (attackHintPanel != null)
            attackHintPanel.SetActive(false);
    }

    /// <summary>
    /// 设置战斗计算器（外部实现
    /// </summary>
    public void SetBattleCalculator(IBattleCalculator calculator)
    {
        battleCalculator = calculator;
    }
}
