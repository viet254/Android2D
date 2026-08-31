using System;
using UnityEngine;

[Serializable]
public class EquipmentSlot
{
    [SerializeField] private EquipmentSlotType slotType;
    [SerializeField] private ItemData equippedItem;

    public EquipmentSlotType SlotType => slotType;
    public ItemData EquippedItem => equippedItem;
    public bool IsEmpty => equippedItem == null;

    public EquipmentSlot(EquipmentSlotType slotType)
    {
        this.slotType = slotType;
    }

    internal void SetItem(ItemData item)
    {
        equippedItem = item;
    }

    internal void Clear()
    {
        equippedItem = null;
    }
}
