using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ItemRegistry", menuName = "Android2D/Save/Item Registry")]
public class ItemRegistry : ScriptableObject
{
    [SerializeField] private List<ItemData> items = new List<ItemData>();

    private readonly Dictionary<string, ItemData> lookup =
        new Dictionary<string, ItemData>(StringComparer.Ordinal);
    private bool lookupDirty = true;

    public IReadOnlyList<ItemData> Items => items;

    public bool TryResolve(string itemId, out ItemData item)
    {
        item = null;
        if (string.IsNullOrWhiteSpace(itemId) || !EnsureLookup(out _))
            return false;

        return lookup.TryGetValue(itemId, out item);
    }

    public bool ValidateRegistry(out string error)
    {
        lookupDirty = true;
        return EnsureLookup(out error);
    }

    private void OnEnable()
    {
        lookupDirty = true;
    }

    private void OnValidate()
    {
        lookupDirty = true;
    }

    private bool EnsureLookup(out string error)
    {
        error = null;
        if (!lookupDirty)
            return true;

        lookup.Clear();
        if (items == null || items.Count == 0)
        {
            error = "ItemRegistry contains no items.";
            return false;
        }

        for (int i = 0; i < items.Count; i++)
        {
            ItemData item = items[i];
            if (item == null)
            {
                error = $"ItemRegistry entry {i} is null.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(item.ID))
            {
                error = $"Item asset '{item.name}' has an empty ID.";
                return false;
            }

            if (lookup.ContainsKey(item.ID))
            {
                error = $"Duplicate item ID '{item.ID}' in ItemRegistry.";
                return false;
            }

            lookup.Add(item.ID, item);
        }

        lookupDirty = false;
        return true;
    }
}
