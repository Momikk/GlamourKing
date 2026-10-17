using FFXIVClientStructs.FFXIV.Client.Game;
using Lumina.Excel.Sheets;

namespace SuitKing;

internal static class DresserOutfitReader
{
    // Read the dresser only after its server data has loaded. Loose items are not saved outfits.
    public static unsafe IReadOnlyList<StoredOutfit>? Read()
    {
        var manager = MirageManager.Instance();
        if (manager == null || !manager->PrismBoxLoaded)
            return null;
        var sheet = Plugin.Data.GetExcelSheet<MirageStoreSetItem>();
        var result = new List<StoredOutfit>();
        var ids = manager->PrismBoxItemIds;
        for (var index = 0; index < ids.Length; index++)
        {
            var rawId = ids[index];
            if (rawId == 0 || rawId >= 2_000_000)
                continue;
            var id = rawId % 1_000_000;
            if (!sheet.TryGetRow(id, out var set))
                continue;
            var pieces = new HashSet<uint>();
            var slots = OutfitCatalog.PiecesInOrder(set);
            for (var slot = 0; slot < slots.Length; slot++)
            {
                if (slots[slot] != 0 && manager->IsSetSlotUnlocked((uint)index, slot))
                    pieces.Add(slots[slot]);
            }
            result.Add(new(id, pieces, rawId >= 1_000_000, index));
        }
        return manager->PrismBoxLoaded ? result : null;
    }
}
