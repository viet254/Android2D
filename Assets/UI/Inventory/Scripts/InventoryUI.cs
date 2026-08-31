using System.Collections.Generic;
using UnityEngine;

public class InventoryUI : MonoBehaviour
{
    [SerializeField] private Inventory inventory;
    [SerializeField] private Equipment equipment;
    [SerializeField] private Transform slotsRoot;
    [SerializeField] private InventorySlotUI slotTemplate;

    private readonly List<InventorySlotUI> slotViews = new List<InventorySlotUI>();

    private void OnEnable()
    {
        if (!Application.isPlaying)
            return;

        ResolveSources();
        Subscribe();
        Refresh();
    }

    private void OnDisable()
    {
        if (inventory != null)
            inventory.OnInventoryChanged -= Refresh;
    }

    public void Refresh()
    {
        if (inventory == null || slotsRoot == null || slotTemplate == null)
            return;

        EnsureSlotViewCount(inventory.Slots.Count);
        for (int i = 0; i < slotViews.Count; i++)
            slotViews[i].Bind(inventory, i, inventory.Slots[i], equipment);
    }

    private void ResolveSources()
    {
        if (inventory != null && equipment != null)
            return;

        PlayerController player = FindFirstObjectByType<PlayerController>();
        if (player == null)
            return;

        if (inventory == null)
            inventory = player.GetComponent<Inventory>();
        if (equipment == null)
            equipment = player.GetComponent<Equipment>();
    }

    private void Subscribe()
    {
        if (inventory == null)
        {
            Debug.LogWarning("[InventoryUI] Inventory source was not found.", this);
            return;
        }

        inventory.OnInventoryChanged -= Refresh;
        inventory.OnInventoryChanged += Refresh;
    }

    private void EnsureSlotViewCount(int requiredCount)
    {
        while (slotViews.Count < requiredCount)
        {
            InventorySlotUI view = Instantiate(slotTemplate, slotsRoot);
            view.gameObject.name = $"Inventory Slot {slotViews.Count + 1}";
            view.gameObject.SetActive(true);
            slotViews.Add(view);
        }

        while (slotViews.Count > requiredCount)
        {
            int lastIndex = slotViews.Count - 1;
            InventorySlotUI view = slotViews[lastIndex];
            slotViews.RemoveAt(lastIndex);
            Destroy(view.gameObject);
        }
    }
}
