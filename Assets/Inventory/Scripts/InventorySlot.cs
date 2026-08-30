using System;
using UnityEngine;

[Serializable]
public class InventorySlot : ISerializationCallbackReceiver
{
    [SerializeField] private ItemData item;
    [SerializeField, Min(0)] private int quantity;

    public ItemData Item => item;
    public int Quantity => quantity;
    public bool IsEmpty => item == null || quantity == 0;

    public InventorySlot()
    {
        Clear();
    }

    public InventorySlot(ItemData item, int quantity)
    {
        SetItem(item, quantity);
    }

    public void SetItem(ItemData newItem, int newQuantity = 1)
    {
        item = newItem;
        quantity = item == null
            ? 0
            : Mathf.Clamp(newQuantity, 0, item.MaxStack);

        if (quantity == 0)
            item = null;
    }

    public int AddQuantity(int amount)
    {
        if (IsEmpty || amount <= 0)
            return 0;

        int added = Mathf.Min(amount, item.MaxStack - quantity);
        quantity += added;
        return added;
    }

    public int RemoveQuantity(int amount)
    {
        if (IsEmpty || amount <= 0)
            return 0;

        int removed = Mathf.Min(amount, quantity);
        quantity -= removed;

        if (quantity == 0)
            item = null;

        return removed;
    }

    public void Clear()
    {
        item = null;
        quantity = 0;
    }

    public void OnBeforeSerialize()
    {
        Normalize();
    }

    public void OnAfterDeserialize()
    {
        Normalize();
    }

    private void Normalize()
    {
        if (item == null)
        {
            quantity = 0;
            return;
        }

        quantity = Mathf.Clamp(quantity, 0, item.MaxStack);
        if (quantity == 0)
            item = null;
    }
}
