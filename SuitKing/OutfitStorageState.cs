namespace SuitKing;

internal enum OutfitStorageState { Unknown, Saved, CanComplete, NotCreated, CanStoreCabinet, QualityConflict }
internal sealed record OutfitDefinition(uint Id, string Name, IReadOnlyList<uint> Pieces);
internal sealed record StoredOutfit(uint Id, IReadOnlySet<uint> Pieces, bool HighQuality = false, int BoxIndex = -1);
internal sealed record OutfitAssessment(OutfitStorageState State, string Details);

internal static class OutfitStateEvaluator
{
    public static string Label(OutfitStorageState state) => state switch
    {
        OutfitStorageState.QualityConflict => "品质冲突",
        OutfitStorageState.CanStoreCabinet => "可存收藏柜",
        OutfitStorageState.Saved => "已保存相同套装",
        OutfitStorageState.CanComplete => "可补齐现有套装",
        OutfitStorageState.NotCreated => "尚未建立套装",
        _ => "待确认",
    };

    public static OutfitAssessment Evaluate(uint pieceId, IReadOnlyList<OutfitDefinition>? definitions,
        IReadOnlyList<StoredOutfit>? stored, bool highQuality = false)
    {
        if (stored is null || definitions is null || definitions.Count == 0)
            return new(OutfitStorageState.Unknown, "投影台套装数据尚未就绪。");
        var details = new List<string>();
        var states = new List<OutfitStorageState>();
        foreach (var definition in definitions)
        {
            var all = stored.Where(set => set.Id == definition.Id).ToArray();
            var matches = all.Where(set => set.HighQuality == highQuality).ToArray();
            var state = matches.Length == 0
                ? all.Length == 0 ? OutfitStorageState.NotCreated
                    : all.Any(set => !set.Pieces.Contains(pieceId)) ? OutfitStorageState.QualityConflict : OutfitStorageState.Saved
                : matches.Any(set => !set.Pieces.Contains(pieceId)) ? OutfitStorageState.CanComplete
                : OutfitStorageState.Saved;
            states.Add(state);
            details.Add($"{definition.Name}：{Label(state)}");
        }
        // Prefer an actionable missing slot; details retain other set memberships.
        var overall = states.Contains(OutfitStorageState.CanComplete) ? OutfitStorageState.CanComplete
            : states.Contains(OutfitStorageState.QualityConflict) ? OutfitStorageState.QualityConflict
            : states.Contains(OutfitStorageState.Saved) ? OutfitStorageState.Saved
            : OutfitStorageState.NotCreated;
        return new(overall, string.Join("\n", details));
    }
}
