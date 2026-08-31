using System;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Inventory))]
public class Equipment : MonoBehaviour
{
    [SerializeField] private Inventory inventory;
    [SerializeField] private EquipmentSlot weaponSlot = new EquipmentSlot(EquipmentSlotType.Weapon);

    public event Action OnEquipmentChanged;

    public WeaponData EquippedWeapon => GetEquippedItem(EquipmentSlotType.Weapon) as WeaponData;

    private void Awake()
    {
        ResolveInventory();
        EnsureSlots();
    }

    private void OnValidate()
    {
        ResolveInventory();
        EnsureSlots();
    }

    public ItemData GetEquippedItem(EquipmentSlotType slotType)
    {
        return slotType == EquipmentSlotType.Weapon ? weaponSlot.EquippedItem : null;
    }

    public bool Equip(ItemData item)
    {
        ResolveInventory();
        if (inventory == null || !IsValidWeapon(item) || !inventory.HasItem(item, 1))
            return false;

        ItemData previouslyEquipped = weaponSlot.EquippedItem;
        if (previouslyEquipped == item)
            return false;

        if (!inventory.RemoveItem(item, 1))
            return false;

        if (previouslyEquipped != null && !inventory.TryAddItem(previouslyEquipped, 1))
        {
            bool rollbackSucceeded = inventory.TryAddItem(item, 1);
            if (!rollbackSucceeded)
            {
                Debug.LogError(
                    $"[Equipment] Failed to roll back '{item.DisplayName}' after an equip transaction failure.",
                    this);
            }

            return false;
        }

        weaponSlot.SetItem(item);
        OnEquipmentChanged?.Invoke();
        return true;
    }

    public bool Unequip(EquipmentSlotType slotType)
    {
        ResolveInventory();
        if (inventory == null || slotType != EquipmentSlotType.Weapon || weaponSlot.IsEmpty)
            return false;

        ItemData equippedItem = weaponSlot.EquippedItem;
        if (!inventory.TryAddItem(equippedItem, 1))
            return false;

        weaponSlot.Clear();
        OnEquipmentChanged?.Invoke();
        return true;
    }

    private static bool IsValidWeapon(ItemData item)
    {
        return item is WeaponData && item.ItemType == ItemType.Weapon;
    }

    private void ResolveInventory()
    {
        if (inventory == null)
            inventory = GetComponent<Inventory>();
    }

    private void EnsureSlots()
    {
        if (weaponSlot == null || weaponSlot.SlotType != EquipmentSlotType.Weapon)
            weaponSlot = new EquipmentSlot(EquipmentSlotType.Weapon);
    }
}
