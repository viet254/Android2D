public readonly struct InventoryRestoreEntry
{
    public InventoryRestoreEntry(int slotIndex, ItemData item, int quantity)
    {
        SlotIndex = slotIndex;
        Item = item;
        Quantity = quantity;
    }

    public int SlotIndex { get; }
    public ItemData Item { get; }
    public int Quantity { get; }
}
