using UnityEngine;

public abstract class ItemData : ScriptableObject
{
    [Header("Identity")]
    [SerializeField] private string id = "item";
    [SerializeField] private string displayName = "Item";
    [SerializeField, TextArea] private string description;
    [SerializeField] private Sprite icon;

    [Header("Inventory")]
    [SerializeField] private ItemType itemType;
    [SerializeField] private ItemRarity rarity;
    [SerializeField, Min(1)] private int maxStack = 1;

    public string ID => id;
    public string DisplayName => displayName;
    public string Description => description;
    public Sprite Icon => icon;
    public ItemType ItemType => itemType;
    public ItemRarity Rarity => rarity;
    public int MaxStack => maxStack;

    protected virtual void OnValidate()
    {
        maxStack = Mathf.Max(1, maxStack);
    }
}
