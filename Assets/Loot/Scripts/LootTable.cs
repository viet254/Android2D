using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "LootTable", menuName = "Android2D/Loot/Loot Table")]
public class LootTable : ScriptableObject
{
    [SerializeField] private List<LootEntry> entries = new List<LootEntry>();

    public IReadOnlyList<LootEntry> Entries => entries;

    public List<LootResult> Roll()
    {
        List<LootResult> results = new List<LootResult>();
        if (entries == null)
            return results;

        for (int i = 0; i < entries.Count; i++)
        {
            LootEntry entry = entries[i];
            if (entry != null && entry.TryRoll(out LootResult result))
                results.Add(result);
        }

        return results;
    }

    public bool HasValidEntries()
    {
        if (entries == null || entries.Count == 0)
            return false;

        for (int i = 0; i < entries.Count; i++)
        {
            if (entries[i] != null && entries[i].IsValid)
                return true;
        }

        return false;
    }

    private void OnValidate()
    {
        if (entries == null)
            entries = new List<LootEntry>();

        for (int i = 0; i < entries.Count; i++)
            entries[i]?.Validate();
    }
}
