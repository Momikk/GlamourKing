using FFXIVClientStructs.FFXIV.Client.Game;
using Lumina.Excel.Sheets;

namespace SuitKing;

internal sealed class OutfitInventoryReader
{
    private static readonly (InventoryType Type, string Name)[] Containers =
    [
        (InventoryType.Inventory1, "物品栏 1"), (InventoryType.Inventory2, "物品栏 2"),
        (InventoryType.Inventory3, "物品栏 3"), (InventoryType.Inventory4, "物品栏 4"),
        (InventoryType.ArmoryMainHand, "兵装库·主手"), (InventoryType.ArmoryOffHand, "兵装库·副手"),
        (InventoryType.ArmoryHead, "兵装库·头部"), (InventoryType.ArmoryBody, "兵装库·身体"),
        (InventoryType.ArmoryHands, "兵装库·手部"), (InventoryType.ArmoryLegs, "兵装库·腿部"),
        (InventoryType.ArmoryFeets, "兵装库·脚部"), (InventoryType.ArmoryEar, "兵装库·耳部"),
        (InventoryType.ArmoryNeck, "兵装库·颈部"), (InventoryType.ArmoryWrist, "兵装库·腕部"),
        (InventoryType.ArmoryRings, "兵装库·戒指"),
    ];
    private OutfitCatalog? catalog;
    public IReadOnlyList<OutfitEquipment> Equipment { get; private set; } = [];
    public string Status { get; private set; } = "打开投影台后读取物品栏和兵装库。";
    public bool IsComplete { get; private set; }
    public DateTimeOffset? UpdatedAt { get; private set; }

    public void Clear()
    {
        Equipment = [];
        IsComplete = false;
        UpdatedAt = null;
        Status = "打开投影台后读取物品栏和兵装库。";
    }

    public void Fail(string message)
    {
        Clear();
        Status = message;
    }

    // Only called from Framework.Update. No pointer survives this call and no game memory is written.
    public unsafe void Refresh()
    {
        var manager = InventoryManager.Instance();
        if (manager == null)
        {
            Fail("物品数据尚未就绪，等待加载。" );
            return;
        }
        var entries = new List<InventoryEntry>();
        var missing = new List<string>();
        foreach (var (type, name) in Containers)
        {
            var container = manager->GetInventoryContainer(type);
            if (container == null || !container->IsLoaded)
            {
                missing.Add(name);
                continue;
            }
            if (container->Type != type || container->Size < 0 || container->Size > 140
                || (container->Size != 0 && container->Items == null))
                throw new InvalidOperationException($"{name}数据结构异常，已停止读取。");
            for (var slot = 0; slot < container->Size; slot++)
            {
                var item = container->Items + slot;
                // These real inventory containers should never contain symbolic proxy items.
                if (item->IsSymbolic)
                    throw new InvalidOperationException($"{name}存在未预期的代理物品，已停止读取。");
                if (item->ItemId == 0 || item->Quantity <= 0)
                    continue;
                entries.Add(new(item->ItemId,
                    (item->Flags & InventoryItem.ItemFlags.HighQuality) != 0, name, slot + 1, item->Condition));
            }
        }
        catalog ??= new(Plugin.Data.GetExcelSheet<MirageStoreSetItemLookup>(), Plugin.Data.GetExcelSheet<Item>());
        Equipment = OutfitListBuilder.Build(entries, catalog.Get);
        IsComplete = missing.Count == 0;
        UpdatedAt = DateTimeOffset.Now;
        Status = IsComplete
            ? $"已扫描全部物品栏和兵装库，找到 {Equipment.Count} 件套装幻影化装备。"
            : $"当前结果不完整，等待加载：{string.Join("、", missing)}";
    }

}
