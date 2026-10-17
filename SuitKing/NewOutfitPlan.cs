namespace SuitKing;

internal sealed record NewOutfitPlan(OutfitDefinition Definition, bool HighQuality, IReadOnlyList<OutfitEquipment> Items)
{
    internal uint RawId => Definition.Id + (HighQuality ? 1_000_000u : 0u);
    internal static IReadOnlyList<NewOutfitPlan> Build(IEnumerable<OutfitEquipment> equipment, Func<uint, OutfitInfo?> catalog)
    {
        var candidates = equipment.Where(item => item.Assessment?.State == OutfitStorageState.NotCreated
            && item.Condition >= 30000 && item.RegisteredToGearset.HasValue).OrderBy(item => item.HighQuality).ToArray();
        var used = new HashSet<OutfitEquipment>();
        var planned = new HashSet<uint>();
        var result = new List<NewOutfitPlan>();
        foreach (var source in candidates)
        {
            if (used.Contains(source)) continue;
            var definition = catalog(source.ItemId)?.Sets?.Where(set => set.Id != 0 && set.Pieces.Count == 11
                && set.Pieces.Contains(source.ItemId) && !planned.Contains(set.Id)).OrderBy(set => set.Id).FirstOrDefault();
            if (definition == null) continue;
            var items = candidates.Where(item => !used.Contains(item) && item.HighQuality == source.HighQuality
                && definition.Pieces.Contains(item.ItemId)).GroupBy(item => item.ItemId).Select(group => group.First()).ToArray();
            if (items.Length == 0) continue;
            result.Add(new(definition,source.HighQuality,items)); planned.Add(definition.Id);
            foreach (var item in items) used.Add(item);
        }
        return result;
    }
}
