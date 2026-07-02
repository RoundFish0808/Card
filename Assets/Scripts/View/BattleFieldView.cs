using UnityEngine;
using System;
using System.Collections.Generic;

public class BattleFieldView : MonoBehaviour
{
    [Header("玩家槽位 (3个)")]
    public SlotView[] playerSlots;

    [Header("NPC槽位 (3个)")]
    public SlotView[] npcSlots;

    [Header("攻击系统")]
    public AttackSystem attackSystem;

    [Header("默认模式：放置/攻击")]
    public bool defaultIsPlacementMode = true;

    public bool isPlayerTurn = true;
    public bool isPlacementMode = true;  // true=放置模式 false=攻击模式

    public event Action<int, CardView> OnPlayerPlacedCard;

    private CardView selectedCard;

    private void Awake()
    {   
        //初始化槽模式
        isPlacementMode = defaultIsPlacementMode;

        //初始化  玩家槽 监听事件
        for (int i = 0; i < playerSlots.Length; i++)
        {
            playerSlots[i].slotIndex = i;
            playerSlots[i].isPlayerSlot = true;
            playerSlots[i].OnSlotClicked += HandlePlayerSlotClicked;
            playerSlots[i].OnSlotEnter += HandleSlotEnter;
            playerSlots[i].OnSlotExit += HandleSlotExit;
            playerSlots[i].OnCardPlaced += HandleCardPlacedToSlot;
        }
        
        //初始化  NPC槽 监听事件
        for (int i = 0; i < npcSlots.Length; i++)
        {
            npcSlots[i].slotIndex = i;
            npcSlots[i].isPlayerSlot = false;
            npcSlots[i].OnSlotClicked += HandleEnemySlotClicked;
            npcSlots[i].SetInteractable(false);
        }

        if (attackSystem != null)
        {
            attackSystem.battleField = this;
        }

        SetSlotsInteractable(isPlayerTurn);
    }

    private void OnDestroy()
    {
        foreach (var slot in playerSlots)
        {
            slot.OnSlotClicked -= HandlePlayerSlotClicked;
            slot.OnSlotEnter -= HandleSlotEnter;
            slot.OnSlotExit -= HandleSlotExit;
            slot.OnCardPlaced -= HandleCardPlacedToSlot;
        }

        foreach (var slot in npcSlots)
        {
            slot.OnSlotClicked -= HandleEnemySlotClicked;
        }
    }

    //选择槽位卡片(在拖拽时会调用)
    public void SetSelectedCard(CardView card)
    {
        selectedCard = card;
    }

    /// <summary>
    /// 切换模式：放置模式 / 攻击模式
    /// </summary>
    public void SetMode(bool placementMode)
    {
        isPlacementMode = placementMode;

        if (attackSystem != null)
        {
            if (!placementMode)
            {
                attackSystem.EnterAttackMode();
            }
            else
            {
                attackSystem.ExitAttackMode();
            }
        }

        UpdateSlotInteractivity();
    }

    public void SetSlotsInteractable(bool interactable)
    {
        isPlayerTurn = interactable;
        UpdateSlotInteractivity();
    }

    private void UpdateSlotInteractivity()
    {
        foreach (var slot in playerSlots)
        {
            slot.SetInteractable(isPlayerTurn);
        }

        // 敌方槽位只有在攻击模式下才可交互
        foreach (var slot in npcSlots)
        {
            slot.SetInteractable(isPlayerTurn && !isPlacementMode);
        }
    }

    // 玩家槽位被点击
    private void HandlePlayerSlotClicked(SlotView slot)
    {
        if (!isPlayerTurn) return;

        if (!isPlacementMode && attackSystem != null)
        {
            // 攻击模式：选择主攻牌
            attackSystem.OnPlayerSlotClicked(slot);
        }
        else
        {
            // 放置模式：点击槽位没用（需要拖拽）
        }
    }

    // 敌方槽位被点击
    private void HandleEnemySlotClicked(SlotView slot)
    {
        if (!isPlayerTurn) return;
        if (isPlacementMode) return;
        if (attackSystem == null) return;

        // 攻击模式：选择目标
        attackSystem.OnEnemySlotClicked(slot);
    }

    private void HandleSlotEnter(SlotView slot)
    {
        // 高亮现在由 CardDragHandler 在拖拽过程中自动处理
        // 这里只处理非拖拽状态的悬停逻辑（如攻击模式下选择目标时的预览）
        // if (selectedCard == null || slot.IsOccupied) return;
        // slot.SetHighlighted(true);
    }

    private void HandleSlotExit(SlotView slot)
    {
        // 高亮现在由 CardDragHandler 在拖拽过程中自动处理
        // slot.SetHighlighted(false);
    }

    private void HandleCardPlacedToSlot(SlotView slot, CardView card)
    {
        OnPlayerPlacedCard?.Invoke(slot.slotIndex, card);

        // 玩家槽位一旦有牌，就立即进入攻击模式。
        // 这里放在 BattleFieldView 内部处理，避免依赖 HandPresenter 的创建/订阅流程，导致牌已放下但点击没有高亮。
        if (slot.isPlayerSlot && slot.IsOccupied && HasOccupiedNPCSlot())
        {
            Debug.Log($"[BattleFieldView] 玩家槽位 {slot.slotIndex} 已放牌，切换到攻击模式。");
            SetMode(false);
        }
    }

    private bool HasOccupiedNPCSlot()
    {
        if (npcSlots == null) return false;

        foreach (var slot in npcSlots)
        {
            if (slot != null && slot.IsOccupied)
                return true;
        }

        return false;
    }

    public void PlaceCardToNPCSlot(int slotIndex, CardView card, bool withAnimation = true)
    {
        if (slotIndex < 0 || slotIndex >= npcSlots.Length) return;
        npcSlots[slotIndex].PlaceCard(card, withAnimation);
    }

    public void PlaceCardToPlayerSlot(int slotIndex, CardView card, bool withAnimation = true)
    {
        if (slotIndex < 0 || slotIndex >= playerSlots.Length) return;
        playerSlots[slotIndex].PlaceCard(card, withAnimation);
    }

    public CardView GetCardFromPlayerSlot(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= playerSlots.Length) return null;
        return playerSlots[slotIndex].CurrentCard;
    }

    public CardView GetCardFromNPCSlot(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= npcSlots.Length) return null;
        return npcSlots[slotIndex].CurrentCard;
    }

    public int GetFirstEmptyPlayerSlot()
    {
        for (int i = 0; i < playerSlots.Length; i++)
        {
            if (!playerSlots[i].IsOccupied) return i;
        }
        return -1;
    }

    public int GetFirstEmptyNPCSlot()
    {
        for (int i = 0; i < npcSlots.Length; i++)
        {
            if (!npcSlots[i].IsOccupied) return i;
        }
        return -1;
    }

    public List<int> GetEmptyPlayerSlots()
    {
        List<int> list = new List<int>();
        for (int i = 0; i < playerSlots.Length; i++)
        {
            if (!playerSlots[i].IsOccupied) list.Add(i);
        }
        return list;
    }

    public List<int> GetEmptyNPCSlots()
    {
        List<int> list = new List<int>();
        for (int i = 0; i < npcSlots.Length; i++)
        {
            if (!npcSlots[i].IsOccupied) list.Add(i);
        }
        return list;
    }

    public bool HasEmptyPlayerSlots()
    {
        return GetFirstEmptyPlayerSlot() >= 0;
    }

    public void ClearAllSlots()
    {
        foreach (var slot in playerSlots)
            slot.RemoveCard();

        foreach (var slot in npcSlots)
            slot.RemoveCard();
    }
}
