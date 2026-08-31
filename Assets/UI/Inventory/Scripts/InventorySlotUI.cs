using UnityEngine;
using UnityEngine.UI;

public class InventorySlotUI : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private Image iconImage;
    [SerializeField] private Text quantityText;
    [SerializeField] private Text emptyText;

    private Inventory inventory;
    private InventorySlot slot;
    private int slotIndex = -1;
    private Equipment equipment;

    private void OnEnable()
    {
        if (button != null)
            button.onClick.AddListener(HandleClick);
    }

    private void OnDisable()
    {
        if (button != null)
            button.onClick.RemoveListener(HandleClick);
    }

    public void Bind(
        Inventory sourceInventory,
        int sourceSlotIndex,
        InventorySlot sourceSlot,
        Equipment sourceEquipment)
    {
        inventory = sourceInventory;
        slotIndex = sourceSlotIndex;
        slot = sourceSlot;
        equipment = sourceEquipment;
        Refresh();
    }

    public void Refresh()
    {
        ItemData item = slot != null && !slot.IsEmpty ? slot.Item : null;
        bool hasItem = item != null;

        if (iconImage != null)
        {
            iconImage.sprite = hasItem ? item.Icon : null;
            iconImage.enabled = hasItem && item.Icon != null;
        }

        if (quantityText != null)
        {
            bool showQuantity = hasItem && slot.Quantity > 1;
            quantityText.gameObject.SetActive(showQuantity);
            quantityText.text = showQuantity ? slot.Quantity.ToString() : string.Empty;
        }

        if (emptyText != null)
            emptyText.gameObject.SetActive(!hasItem);

        if (button != null)
        {
            bool canEquip = item is WeaponData && equipment != null;
            bool canUse = item is ConsumableData
                && item.ItemType == ItemType.Consumable
                && inventory != null;
            button.interactable = hasItem && (canEquip || canUse);
        }
    }

    private void HandleClick()
    {
        ItemData item = slot != null && !slot.IsEmpty ? slot.Item : null;
        if (item is WeaponData && equipment != null)
        {
            equipment.Equip(item);
            Refresh();
            return;
        }

        if (item is ConsumableData
            && item.ItemType == ItemType.Consumable
            && inventory != null)
        {
            inventory.TryUseItem(slotIndex, inventory.gameObject);
        }
    }
}
