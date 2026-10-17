namespace SuitKing;

internal static class OutfitSubmissionLayout
{
    // Native StoreNewOutfit reads at most nine entries and stops at the first Invalid
    // container. Empty sheet columns must be omitted, not represented as leading holes.
    internal const int Capacity = 9;
    internal static CompletionItemKey[] Pack(IReadOnlyList<uint> columns, IReadOnlyList<CompletionItemKey> sources, bool highQuality)
    {
        if (columns.Count != 11 || sources.Count == 0 || sources.Count > Capacity)
            throw new InvalidOperationException("套装提交数量或部位结构异常。");
        if (sources.Select(key => key.ItemId).Distinct().Count() != sources.Count
            || sources.Select(key => (key.Container,key.Slot)).Distinct().Count() != sources.Count)
            throw new InvalidOperationException("套装提交包含重复部件或来源。");
        var ranked = new List<(int Index,CompletionItemKey Key)>();
        foreach (var key in sources)
        {
            var index = Array.IndexOf(columns.ToArray(),key.ItemId);
            if (key.ItemId == 0 || index < 0 || key.Container == 9999 || key.HighQuality != highQuality)
                throw new InvalidOperationException("套装提交部件、来源或品质不符。");
            ranked.Add((index,key));
        }
        return ranked.OrderBy(item => item.Index).Select(item => item.Key).ToArray();
    }
}
