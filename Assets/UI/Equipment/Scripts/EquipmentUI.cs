using UnityEngine;

public class EquipmentUI : MonoBehaviour
{
    [SerializeField] private Equipment equipment;
    [SerializeField] private EquipmentSlotUI weaponSlotUI;

    private void OnEnable()
    {
        if (!Application.isPlaying)
            return;

        ResolveSource();
        Subscribe();
        Refresh();
    }

    private void OnDisable()
    {
        if (equipment != null)
            equipment.OnEquipmentChanged -= Refresh;
    }

    public void Refresh()
    {
        if (weaponSlotUI != null)
            weaponSlotUI.Bind(equipment, EquipmentSlotType.Weapon);
    }

    private void ResolveSource()
    {
        if (equipment != null)
            return;

        PlayerController player = FindFirstObjectByType<PlayerController>();
        if (player != null)
            equipment = player.GetComponent<Equipment>();
    }

    private void Subscribe()
    {
        if (equipment == null)
        {
            Debug.LogWarning("[EquipmentUI] Equipment source was not found.", this);
            return;
        }

        equipment.OnEquipmentChanged -= Refresh;
        equipment.OnEquipmentChanged += Refresh;
    }
}
