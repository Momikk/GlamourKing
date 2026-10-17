namespace SuitKing;

internal sealed record InventoryEntry(uint ItemId, bool HighQuality, string Location, int Slot, ushort Condition, bool? RegisteredToGearset = null);
internal sealed record OutfitInfo(string ItemName, string SetNames, IReadOnlyList<OutfitDefinition>? Sets = null, uint IconId = 0, string Part = "其他");
internal sealed record OutfitEquipment(uint ItemId, string Name, bool HighQuality, string Location, int Slot, ushort Condition, string SetNames, bool? RegisteredToGearset = null, OutfitAssessment? Assessment = null, uint IconId = 0, string Part = "其他", uint? CabinetId = null, bool? InCabinet = null);

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
                entry.Slot, entry.Condition, info.SetNames, entry.RegisteredToGearset, IconId: info.IconId, Part: info.Part));
        }
        return result.OrderBy(item => EquipmentParts.Rank(item.Part)).ThenBy(item => item.Name, StringComparer.Ordinal)
            .ThenBy(item => item.Location, StringComparer.Ordinal).ThenBy(item => item.Slot).ToArray();
    }
}

internal static class OutfitDisplayFilter
{
    public static IReadOnlyList<OutfitEquipment> Apply(IEnumerable<OutfitEquipment> equipment,
        bool hideGearsetItems, string search)
        => equipment.Where(item => (!hideGearsetItems || item.RegisteredToGearset == false)
            && (string.IsNullOrWhiteSpace(search)
                || item.Name.Contains(search, StringComparison.OrdinalIgnoreCase)
                || item.SetNames.Contains(search, StringComparison.OrdinalIgnoreCase))).ToArray();
}

internal static class GearsetRegistration
{
    internal static bool IsRegistered(IReadOnlySet<uint> registeredIds, uint itemId, bool highQuality)
        => registeredIds.Contains(itemId + (highQuality ? 1_000_000u : 0u));
}
internal sealed class GearsetSnapshotGate
{
    private HashSet<uint>? previous;
    internal bool IsStable(IReadOnlySet<uint> ids)
    {
        var stable = previous is not null && previous.SetEquals(ids);
        previous = new HashSet<uint>(ids);
        return stable;
    }
    internal void Clear() => previous = null;
}