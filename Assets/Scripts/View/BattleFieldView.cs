using UnityEngine;
using System;
using System.Collections.Generic;

public class BattleFieldView : MonoBehaviour
{
    [Header("玩家槽位 (3个)")]
    public SlotView[] playerSlots;

    [Header("NPC槽位 (3个)")]
    public SlotView[] npcSlots;

    [Header("是否是玩家回合")]
    public bool isPlayerTurn = true;

    public event Action<int, CardView> OnPlayerPlacedCard;

    private CardView selectedCard;

    private void Awake()
    {
        for (int i = 0; i < playerSlots.Length; i++)
        {
            playerSlots[i].slotIndex = i;
            playerSlots[i].isPlayerSlot = true;
            playerSlots[i].OnSlotClicked += HandleSlotClicked;
            playerSlots[i].OnSlotEnter += HandleSlotEnter;
            playerSlots[i].OnSlotExit += HandleSlotExit;
        }

        for (int i = 0; i < npcSlots.Length; i++)
        {
            npcSlots[i].slotIndex = i;
            npcSlots[i].isPlayerSlot = false;
            npcSlots[i].SetInteractable(false);
        }

        SetSlotsInteractable(isPlayerTurn);
    }

    private void OnDestroy()
    {
        foreach (var slot in playerSlots)
        {
            slot.OnSlotClicked -= HandleSlotClicked;
            slot.OnSlotEnter -= HandleSlotEnter;
            slot.OnSlotExit -= HandleSlotExit;
        }
    }

    public void SetSelectedCard(CardView card)
    {
        selectedCard = card;
    }

    public void SetSlotsInteractable(bool interactable)
    {
        isPlayerTurn = interactable;
        foreach (var slot in playerSlots)
        {
            slot.SetInteractable(interactable);
        }
    }

    private void HandleSlotClicked(SlotView slot)
    {
        if (selectedCard == null) return;
        if (slot.IsOccupied) return;

        slot.PlaceCard(selectedCard);
        OnPlayerPlacedCard?.Invoke(slot.slotIndex, selectedCard);
        selectedCard = null;
    }

    private void HandleSlotEnter(SlotView slot)
    {
        if (selectedCard == null || slot.IsOccupied) return;
        slot.SetHighlighted(true);
    }

    private void HandleSlotExit(SlotView slot)
    {
        slot.SetHighlighted(false);
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

    public void ClearAllSlots()
    {
        foreach (var slot in playerSlots)
            slot.RemoveCard();

        foreach (var slot in npcSlots)
            slot.RemoveCard();
    }
}
