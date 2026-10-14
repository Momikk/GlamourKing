using SuitKing;
using Lumina;
using Lumina.Excel.Sheets;

var catalog = new Dictionary<uint, OutfitInfo>
{
    [10] = new("套装上衣", "套装甲"),
    [20] = new("套装戒指", "套装乙、套装丙"),
};
OutfitInfo? Find(uint id) => catalog.GetValueOrDefault(id);
var fixture = new List<InventoryEntry>
{
    new(10, false, "物品栏 1", 1, 30000),
    new(10, true, "物品栏 4", 35, 30000),
    new(20, false, "兵装库·戒指", 50, 10000),
    new(999, false, "兵装库·身体", 2, 30000),
    new(0, false, "物品栏 2", 1, 30000),
    new(10, false, "兵装库·主手", 2, 30000),
};
var result = OutfitListBuilder.Build(fixture, Find);
void Check(bool ok, string name)
{
    if (!ok) throw new Exception(name);
    Console.WriteLine($"PASS: {name}");
}
Check(result.Count == 4, "All supplied pages included; ordinary and empty slots excluded");
Check(result.Count(x => x.ItemId == 10) == 3, "Duplicate owned copies retained");
Check(result.Any(x => x.HighQuality && x.Location == "物品栏 4" && x.Slot == 35), "HQ and inventory position retained");
Check(result.Any(x => x.Location == "兵装库·主手"), "Weapon category included");
Check(result.Single(x => x.ItemId == 20).SetNames.Contains("套装丙"), "Multiple outfit memberships retained");
Check(result.Single(x => x.ItemId == 20).Condition == 10000, "Damaged gear remains identifiable instead of silently disappearing");
Check(OutfitListBuilder.Build([], Find).Count == 0, "Empty inventory handled");
Check(OutfitListBuilder.Build(fixture.Where(x => x.ItemId != 10), Find).Count == 1, "Removed items disappear on refreshed snapshot");

var thirtySix = Enumerable.Range(1, 36).Select(i => new OutfitEquipment((uint)i, $"装备{i}",
    i % 2 == 0, "兵装库·身体", i, 30000, "测试套装", i <= 6)).ToArray();
Check(OutfitDisplayFilter.Apply(thirtySix, false, "").Count == 36, "Gearset filter off retains all 36 entries");
Check(OutfitDisplayFilter.Apply(thirtySix, true, "").Count == 30, "Gearset filter on hides exactly six registered entries");
Check(OutfitDisplayFilter.Apply(thirtySix, true, "装备1").All(item => item.RegisteredToGearset != true
    && item.Name.Contains("装备1")), "Search and gearset filter apply together");
var copies = new[] { new OutfitEquipment(10, "重复副本", false, "物品栏 1", 1, 30000, "测试套装", true),
    new OutfitEquipment(10, "重复副本", true, "物品栏 2", 1, 30000, "测试套装", false) };
Check(OutfitDisplayFilter.Apply(copies, true, "").Single().HighQuality, "Filter uses per-copy registration instead of item-id membership");
Check(OutfitDisplayFilter.Apply([copies[0]], true, "").Count == 0, "Fully filtered inventory is empty");
Check(OutfitDisplayFilter.Apply([copies[1] with { RegisteredToGearset = null }], true, "").Count == 1,
    "Unknown registration is not silently discarded");
if (args.Length == 0) return;
using var game = new GameData(args[0], new LuminaOptions { DefaultExcelLanguage = Lumina.Data.Language.ChineseSimplified });
var items = game.Excel.GetSheet<Item>(Lumina.Data.Language.ChineseSimplified);
var lookup = game.Excel.GetSheet<MirageStoreSetItemLookup>(Lumina.Data.Language.ChineseSimplified);
var names = new[] { "帕戈尔赞治愈胸衣", "战女神审言胸甲", "改良型斯卡艾瓦咏咒长衣" };
foreach (var name in names)
{
    var found = items.FirstOrDefault(row => row.Name.ExtractText() == name);
    if (found.RowId == 0)
    {
        Console.WriteLine($"SAMPLE NOT FOUND: {name}");
        continue;
    }
    var sets = lookup.TryGetRow(found.RowId, out var mapping)
        ? mapping.Item.Where(set => set.RowId != 0 && set.IsValid).Select(set => set.Value.Name.ExtractText()).ToArray()
        : [];
    Console.WriteLine($"CN SAMPLE: {name} ({found.RowId}) => {(sets.Length == 0 ? "no outfit marker" : string.Join(" / ", sets))}");
}
Check(lookup.Any(row => row.Item.Any(set => set.RowId != 0 && set.IsValid)), "Local CN outfit catalog loads and resolves set names");

var liveCatalog = new OutfitCatalog(lookup, items);
var marked = items.First(row => row.Name.ExtractText() == "帕戈尔赞治愈胸衣");
var ordinary = items.First(row => row.Name.ExtractText() == "改良型斯卡艾瓦咏咒长衣");
Check(liveCatalog.Get(marked.RowId)?.SetNames == "帕戈尔赞治愈套装", "Actual plugin catalog resolves screenshot marked item");
Check(liveCatalog.Get(ordinary.RowId) is null, "Actual plugin catalog excludes screenshot unmarked item");
var scan = OutfitListBuilder.Build([new(marked.RowId, false, "兵装库·身体", 1, 30000),
    new(ordinary.RowId, false, "物品栏 4", 20, 30000)], liveCatalog.Get);
Check(scan.Count == 1 && scan[0].ItemId == marked.RowId, "CN catalog integration selects only marked equipment");
