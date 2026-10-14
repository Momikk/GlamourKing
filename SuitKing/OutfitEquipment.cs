namespace SuitKing;

internal sealed record InventoryEntry(uint ItemId, bool HighQuality, string Location, int Slot, ushort Condition);
internal sealed record OutfitInfo(string ItemName, string SetNames);
internal sealed record OutfitEquipment(uint ItemId, string Name, bool HighQuality, string Location, int Slot, ushort Condition, string SetNames);

internal static class OutfitListBuilder
{
    // Keep each owned copy: merging by item id would lose its inventory location and HQ flag.
    public static IReadOnlyList<OutfitEquipment> Build(IEnumerable<InventoryEntry> inventory,
        Func<uint, OutfitInfo?> findOutfit)
    {
        var result = new List<OutfitEquipment>();
        foreach (var entry in inventory)
        {
            if (entry.ItemId == 0)
                continue;
            var info = findOutfit(entry.ItemId);
            if (info is null)
                continue;
            result.Add(new(entry.ItemId, info.ItemName, entry.HighQuality, entry.Location,
                entry.Slot, entry.Condition, info.SetNames));
        }
        return result.OrderBy(item => item.Name, StringComparer.Ordinal)
            .ThenBy(item => item.Location, StringComparer.Ordinal).ThenBy(item => item.Slot).ToArray();
    }
}
