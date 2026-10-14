using Lumina.Excel;
using Lumina.Excel.Sheets;

namespace SuitKing;

internal sealed class OutfitCatalog(ExcelSheet<MirageStoreSetItemLookup> lookup, ExcelSheet<Item> items)
{
    private readonly Dictionary<uint, OutfitInfo?> cache = new();

    public OutfitInfo? Get(uint itemId)
    {
        if (cache.TryGetValue(itemId, out var cached))
            return cached;
        var setNames = new List<string>();
        if (lookup.TryGetRow(itemId, out var row))
        {
            foreach (var set in row.Item)
            {
                if (set.RowId == 0 || !set.IsValid)
                    continue;
                var name = set.Value.Name.ExtractText();
                if (!string.IsNullOrWhiteSpace(name))
                    setNames.Add(name);
            }
        }
        OutfitInfo? info = null;
        if (setNames.Count != 0 && items.TryGetRow(itemId, out var itemRow))
            info = new(itemRow.Name.ExtractText(), string.Join("、", setNames.Distinct()));
        cache[itemId] = info;
        return info;
    }
}
