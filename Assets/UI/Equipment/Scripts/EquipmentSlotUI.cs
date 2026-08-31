using UnityEngine;
using UnityEngine.UI;

public class EquipmentSlotUI : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private Image iconImage;
    [SerializeField] private Text emptyText;

    private Equipment equipment;
    private EquipmentSlotType slotType;

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

    public void Bind(Equipment sourceEquipment, EquipmentSlotType sourceSlotType)
    {
        equipment = sourceEquipment;
        slotType = sourceSlotType;
        Refresh();
    }

    public void Refresh()
    {
        ItemData item = equipment != null ? equipment.GetEquippedItem(slotType) : null;
        bool hasItem = item != null;

        if (iconImage != null)
        {
            iconImage.sprite = hasItem ? item.Icon : null;
            iconImage.enabled = hasItem && item.Icon != null;
        }

        if (emptyText != null)
            emptyText.gameObject.SetActive(!hasItem);

        if (button != null)
            button.interactable = hasItem && equipment != null;
    }

    private void HandleClick()
    {
        if (equipment == null)
            return;

        equipment.Unequip(slotType);
        Refresh();
    }
}
