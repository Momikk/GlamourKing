using Lumina.Excel;
using Lumina.Excel.Sheets;

namespace SuitKing;

internal sealed class OutfitCatalog(ExcelSheet<MirageStoreSetItemLookup> lookup, ExcelSheet<Item> items)
{
    private readonly Dictionary<uint, OutfitInfo?> cache = new();
    private ExcelSheet<MirageStoreSetItem>? setSheet;

    public OutfitInfo? Get(uint itemId)
    {
        if (cache.TryGetValue(itemId, out var cached))
            return cached;
        var setNames = new List<string>();
        var definitions = new List<OutfitDefinition>();
        setSheet ??= lookup.Module.GetSheet<MirageStoreSetItem>();
        if (lookup.TryGetRow(itemId, out var row))
        {
            foreach (var set in row.Item)
            {
                if (set.RowId == 0 || !set.IsValid)
                    continue;
                var name = set.Value.Name.ExtractText();
                if (!string.IsNullOrWhiteSpace(name))
                {
                    setNames.Add(name);
                    if (setSheet.TryGetRow(set.RowId, out var definition))
                        definitions.Add(new(set.RowId, name, PiecesInOrder(definition)));
                }
            }
        }
        OutfitInfo? info = null;
        if (setNames.Count != 0 && items.TryGetRow(itemId, out var itemRow))
            info = new(itemRow.Name.ExtractText(), string.Join("、", setNames.Distinct()), definitions, itemRow.Icon, GetPart(itemRow));
        cache[itemId] = info;
        return info;
    }
    internal static string GetPart(Item item)
    {
        if (!item.EquipSlotCategory.IsValid) return "其他";
        var slot = item.EquipSlotCategory.Value;
        if (slot.MainHand == 1) return "主手";
        if (slot.OffHand == 1) return "副手";
        if (slot.Head == 1) return "头部";
        if (slot.Body == 1) return "身体";
        if (slot.Gloves == 1) return "手部";
        if (slot.Legs == 1) return "腿部";
        if (slot.Feet == 1) return "脚部";
        if (slot.Ears == 1) return "耳部";
        if (slot.Neck == 1) return "颈部";
        if (slot.Wrists == 1) return "腕部";
        if (slot.FingerL == 1 || slot.FingerR == 1) return "戒指";
        return "其他";
    }

    // Column order is the client's outfit unlock-bit order; keep empty slots in place.
    internal static uint[] PiecesInOrder(MirageStoreSetItem set) =>
    [
        set.MainHand.RowId, set.OffHand.RowId, set.Head.RowId, set.Body.RowId, set.Hands.RowId,
        set.Legs.RowId, set.Feet.RowId, set.Earrings.RowId, set.Necklace.RowId,
        set.Bracelets.RowId, set.Ring.RowId,
    ];
}
