namespace SuitKing;

internal sealed record QualityNormalizationPlan(OutfitDefinition Definition, StoredOutfit Stored,
    IReadOnlyList<OutfitEquipment> Additional)
{
    internal uint RawStoredId => Stored.Id + 1_000_000;
    internal int PrismCount => Stored.Pieces.Count + Additional.Count;
    internal ushort RestoreMask => (ushort)Definition.Pieces.Select((id, slot) =>
        id != 0 && Stored.Pieces.Contains(id) ? 1 << slot : 0).Aggregate(0, (a, b) => a | b);

    internal static QualityNormalizationPlan? First(IReadOnlyList<OutfitEquipment> equipment,
        IReadOnlyList<StoredOutfit> stored, Func<uint, OutfitInfo?> catalog)
    {
        foreach (var source in equipment.Where(item => !item.HighQuality && item.RegisteredToGearset == false
            && item.Condition >= 30000 && item.Assessment?.State == OutfitStorageState.QualityConflict))
        {
            foreach (var definition in catalog(source.ItemId)?.Sets ?? [])
            {
                var records = stored.Where(item => item.Id == definition.Id).ToArray();
                if (definition.Pieces.Count != 11 || records.Length != 1 || !records[0].HighQuality || records[0].BoxIndex is < 0 or >= 800
                    || records[0].Pieces.Count == 0 || records[0].Pieces.Any(id => id == 0 || !definition.Pieces.Contains(id))
                    || records[0].Pieces.Contains(source.ItemId)) continue;
                var extras = equipment.Where(item => !item.HighQuality && item.RegisteredToGearset == false
                    && item.Condition >= 30000 && item.Assessment?.State == OutfitStorageState.QualityConflict
                    && definition.Pieces.Contains(item.ItemId) && !records[0].Pieces.Contains(item.ItemId))
                    .GroupBy(item => item.ItemId).Select(group => group.First()).ToArray();
                if (extras.Length != 0 && extras.Length + records[0].Pieces.Count <= OutfitSubmissionLayout.Capacity)
                    return new(definition, records[0], extras);
            }
        }
        return null;
    }
}
