using System;
using UnityEngine;

[Serializable]
public class LootEntry
{
    [SerializeField] private ItemData item;
    [SerializeField, Range(0f, 1f)] private float dropChance = 1f;
    [SerializeField, Min(1)] private int minQuantity = 1;
    [SerializeField, Min(1)] private int maxQuantity = 1;

    public ItemData Item => item;
    public float DropChance => dropChance;
    public int MinQuantity => minQuantity;
    public int MaxQuantity => maxQuantity;
    public bool IsValid => item != null
        && dropChance >= 0f
        && dropChance <= 1f
        && minQuantity >= 1
        && maxQuantity >= minQuantity;

    public bool TryRoll(out LootResult result)
    {
        result = default;
        if (!IsValid || dropChance <= 0f)
            return false;

        if (dropChance < 1f && UnityEngine.Random.value >= dropChance)
            return false;

        int quantity = UnityEngine.Random.Range(minQuantity, maxQuantity + 1);
        result = new LootResult(item, quantity);
        return true;
    }

    internal void Validate()
    {
        dropChance = Mathf.Clamp01(dropChance);
        minQuantity = Mathf.Max(1, minQuantity);
        maxQuantity = Mathf.Max(minQuantity, maxQuantity);
    }
}

public readonly struct LootResult
{
    public LootResult(ItemData item, int quantity)
    {
        Item = item;
        Quantity = quantity;
    }

    public ItemData Item { get; }
    public int Quantity { get; }
}
