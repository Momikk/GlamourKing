namespace SuitKing;

internal sealed record InventoryEntry(uint ItemId, bool HighQuality, string Location, int Slot, ushort Condition, bool? RegisteredToGearset = null);
internal sealed record OutfitInfo(string ItemName, string SetNames, IReadOnlyList<OutfitDefinition>? Sets = null);
internal sealed record OutfitEquipment(uint ItemId, string Name, bool HighQuality, string Location, int Slot, ushort Condition, string SetNames, bool? RegisteredToGearset = null, OutfitAssessment? Assessment = null);

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
                entry.Slot, entry.Condition, info.SetNames, entry.RegisteredToGearset));
        }
        return result.OrderBy(item => item.Name, StringComparer.Ordinal)
            .ThenBy(item => item.Location, StringComparer.Ordinal).ThenBy(item => item.Slot).ToArray();
    }
}

internal static class OutfitDisplayFilter
{
    public static IReadOnlyList<OutfitEquipment> Apply(IEnumerable<OutfitEquipment> equipment,
        bool hideGearsetItems, string search)
        => equipment.Where(item => (!hideGearsetItems || item.RegisteredToGearset != true)
            && (string.IsNullOrWhiteSpace(search)
                || item.Name.Contains(search, StringComparison.OrdinalIgnoreCase)
                || item.SetNames.Contains(search, StringComparison.OrdinalIgnoreCase))).ToArray();
}
