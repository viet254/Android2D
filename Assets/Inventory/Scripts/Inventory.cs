using System;
using System.Collections.Generic;
using UnityEngine;

public class Inventory : MonoBehaviour
{
    [SerializeField, Min(1)] private int slotCount = 20;
    [SerializeField] private List<InventorySlot> slots = new List<InventorySlot>();

    public int SlotCount => slotCount;
    public IReadOnlyList<InventorySlot> Slots => slots;

    public event Action OnInventoryChanged;

    private void Awake()
    {
        EnsureValidSlots();
    }

    private void OnValidate()
    {
        EnsureValidSlots();
    }

    /// <summary>
    /// Adds as many items as possible and returns the quantity that did not fit.
    /// </summary>
    public int AddItem(ItemData item, int quantity)
    {
        if (quantity <= 0)
            return 0;

        if (item == null)
            return quantity;

        EnsureValidSlots();

        int remaining = quantity;

        for (int i = 0; i < slots.Count && remaining > 0; i++)
        {
            InventorySlot slot = slots[i];
            if (!slot.IsEmpty && slot.Item == item)
                remaining -= slot.AddQuantity(remaining);
        }

        for (int i = 0; i < slots.Count && remaining > 0; i++)
        {
            InventorySlot slot = slots[i];
            if (!slot.IsEmpty)
                continue;

            int amountForSlot = Mathf.Min(remaining, item.MaxStack);
            slot.SetItem(item, amountForSlot);
            remaining -= amountForSlot;
        }

        if (remaining != quantity)
            OnInventoryChanged?.Invoke();

        return remaining;
    }

    /// <summary>
    /// Removes the full requested quantity. Returns false without changing data
    /// when the inventory does not contain enough of the item.
    /// </summary>
    public bool RemoveItem(ItemData item, int quantity)
    {
        if (item == null || quantity <= 0 || !HasItem(item, quantity))
            return false;

        int remaining = quantity;
        for (int i = slots.Count - 1; i >= 0 && remaining > 0; i--)
        {
            InventorySlot slot = slots[i];
            if (!slot.IsEmpty && slot.Item == item)
                remaining -= slot.RemoveQuantity(remaining);
        }

        OnInventoryChanged?.Invoke();
        return true;
    }

    public bool HasItem(ItemData item, int quantity)
    {
        if (quantity <= 0)
            return true;

        return item != null && GetItemCount(item) >= quantity;
    }

    public int GetItemCount(ItemData item)
    {
        if (item == null)
            return 0;

        int total = 0;
        for (int i = 0; i < slots.Count; i++)
        {
            InventorySlot slot = slots[i];
            if (!slot.IsEmpty && slot.Item == item)
                total += slot.Quantity;
        }

        return total;
    }

    private void EnsureValidSlots()
    {
        slotCount = Mathf.Max(1, slotCount);

        if (slots == null)
            slots = new List<InventorySlot>(slotCount);

        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i] == null)
                slots[i] = new InventorySlot();
        }

        while (slots.Count < slotCount)
            slots.Add(new InventorySlot());

        while (slots.Count > slotCount && slots[slots.Count - 1].IsEmpty)
            slots.RemoveAt(slots.Count - 1);

        if (slots.Count > slotCount)
            slotCount = slots.Count;
    }
}
