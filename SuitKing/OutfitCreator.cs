using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;

namespace SuitKing;

internal sealed unsafe class OutfitCreator(Plugin plugin)
{
    private readonly Queue<NewOutfitPlan> pending = new();
    private NewOutfitPlan? current;
    private bool waiting;
    private bool hideGearsets;
    private ulong characterId;
    private long deadline;
    private long nextAction;
    private int completed;
    private int total;
    private int skipped;
    internal bool IsRunning { get; private set; }
    internal string Status { get; private set; } = "";
    internal void ClearStatus() { if (!IsRunning) Status = ""; }

    internal void Toggle()
    {
        if (IsRunning) { Stop("已停止；已提交的建立仍可能完成。"); return; }
        try
        {
            plugin.Quality.ClearStatus(); plugin.Completion.ClearStatus();
            if (!plugin.IsDresserOpen || plugin.IsCabinetOpen || plugin.Completion.IsRunning || plugin.Quality.IsRunning || plugin.CabinetStore.IsRunning)
            { Status = "请打开投影台，并先停止其他收纳操作。"; return; }
            if (HasDialog() || plugin.QualityPreview.IsAllocated) { Status = "请先关闭已有确认或预览窗口。"; return; }
            plugin.Inventory.Refresh();
            if (!plugin.Inventory.IsComplete || !plugin.Inventory.AreOutfitsLoaded) { Status = "等待完整装备与套装数据。"; return; }
            hideGearsets = plugin.Config.HideGearsetItems;
            var plans = NewOutfitPlan.Build(OutfitDisplayFilter.Apply(plugin.Inventory.Equipment,hideGearsets,""), plugin.Inventory.FindOutfit);
            if (plans.Count == 0) { Status = "没有可建立且耐久完整的未建立装备。"; return; }
            pending.Clear(); foreach (var plan in plans) pending.Enqueue(plan);
            completed = skipped = 0; total = plans.Count; characterId = Plugin.Player.ContentId;
            current = null; waiting = false; IsRunning = true; deadline = Environment.TickCount64 + 15_000; nextAction = 0;
            Status = $"准备建立 {total} 套，预计消耗 {plans.Sum(plan => plan.Items.Count)} 个幻象棱晶。";
        }
        catch (Exception error) { Plugin.Log.Error(error,"Outfit creation could not start"); Stop("无法开始建立套装，请查看 /xllog。"); }
    }

    internal void Stop(string reason)
    {
        IsRunning = false; pending.Clear(); current = null;
        Status = $"{reason} 已建立 {completed} / {total} 套。";
        plugin.RequestRefresh();
    }

    internal void Tick()
    {
        if (!IsRunning) return;
        try
        {
            if (!plugin.IsDresserOpen || plugin.IsCabinetOpen || Plugin.Player.ContentId != characterId)
            { Stop("投影台已关闭或角色已切换。"); return; }
            if (plugin.Config.HideGearsetItems != hideGearsets) { Stop("过滤条件已改变。"); return; }
            var now = Environment.TickCount64;
            if (now > deadline) { Stop("等待建立套装超时，未重复提交。"); return; }
            if (now < nextAction) return;
            nextAction = now + 350;
            var manager = MirageManager.Instance();
            if (manager == null || !manager->PrismBoxLoaded) return;
            if (HasDialog()) { Stop("出现其他确认窗口，已停止建立。"); return; }
            if (waiting && current != null)
            {
                for (var index = 0; index < 800; index++)
                {
                    if (manager->PrismBoxItemIds[index] != current.RawId) continue;
                    if (current.Items.Any(item => Matches(item) || !manager->IsSetSlotUnlocked((uint)index,
                        Array.IndexOf(current.Definition.Pieces.ToArray(),item.ItemId)))) return;
                    completed++; waiting = false; current = null; deadline = now + 15_000; plugin.RequestRefresh(); break;
                }
                if (waiting) return;
            }
            plugin.Inventory.Refresh();
            if (!plugin.Inventory.IsComplete || !plugin.Inventory.AreOutfitsLoaded) return;
            while (pending.TryDequeue(out var plan))
            {
                // Recheck all qualities: a newly appearing opposite-quality set is a conflict too.
                var exists = false;
                foreach (var raw in manager->PrismBoxItemIds) if (raw != 0 && raw % 1_000_000 == plan.Definition.Id) { exists = true; break; }
                if (exists) { skipped++; continue; }
                if (!manager->PrismBoxItemIds.Contains(0u)) { Stop("投影台没有空位，未建立。"); return; }
                if (plan.Items.Any(item => !Matches(item) || !Eligible(item))) { skipped++; continue; }
                var inventory = InventoryManager.Instance();
                if (inventory == null || inventory->GetInventoryItemCount(QualityNormalizationPolicy.GlamourPrismId,false,false,false) < plan.Items.Count)
                { Stop("幻象棱晶不足，未建立。"); return; }
                InventoryType* containers = stackalloc InventoryType[11]; ushort* slots = stackalloc ushort[11];
                for (var i = 0; i < 11; i++) { containers[i] = InventoryType.Invalid; slots[i] = 0; }
                foreach (var item in plan.Items)
                {
                    var slot = Array.IndexOf(plan.Definition.Pieces.ToArray(),item.ItemId);
                    if (slot < 0 || !OutfitInventoryReader.TryResolveContainer(item.Location,out var type))
                        throw new InvalidOperationException("装备与目标套装部位不符。");
                    containers[slot] = type; slots[slot] = (ushort)(item.Slot - 1);
                }
                current = plan; waiting = true; deadline = now + 15_000;
                Status = $"正在建立 {completed} / {total} 套：{plan.Definition.Name}，消耗 {plan.Items.Count} 个幻象棱晶。";
                if (!manager->StoreNewOutfit(plan.Definition.Id,containers,slots)) Stop("游戏未接受建立套装，请检查原生收纳条件。");
                return;
            }
            Stop(skipped > 0 ? $"建立结束，跳过 {skipped} 套已变化的装备。" : "建立套装完成。");
        }
        catch (Exception error) { Plugin.Log.Error(error,"Outfit creation stopped"); Stop(error is InvalidOperationException ? error.Message : "建立套装失败，请查看 /xllog。"); }
    }

    private bool Eligible(OutfitEquipment item)
    {
        var snapshot = plugin.Inventory.Equipment.FirstOrDefault(entry => entry.ItemId == item.ItemId && entry.Location == item.Location
            && entry.Slot == item.Slot && entry.HighQuality == item.HighQuality);
        if (snapshot?.Assessment?.State != OutfitStorageState.NotCreated || snapshot.Condition < 30000
            || (hideGearsets && snapshot.RegisteredToGearset != false)) return false;
        var slot = Read(item); var gearsets = RaptureGearsetModule.Instance();
        return slot != null && slot->Condition >= 30000 && (!hideGearsets || (gearsets != null && !gearsets->IsItemRegisteredToGearset(slot)));
    }
    private static InventoryItem* Read(OutfitEquipment item)
    {
        var manager = InventoryManager.Instance();
        if (manager == null || !OutfitInventoryReader.TryResolveContainer(item.Location,out var type)) return null;
        var container = manager->GetInventoryContainer(type);
        return container != null && container->IsLoaded && item.Slot > 0 && item.Slot <= container->Size ? container->GetInventorySlot(item.Slot - 1) : null;
    }
    private static bool Matches(OutfitEquipment item)
    {
        var slot = Read(item);
        return slot != null && !slot->IsSymbolic && slot->Quantity == 1 && slot->ItemId == item.ItemId
            && ((slot->Flags & InventoryItem.ItemFlags.HighQuality) != 0) == item.HighQuality;
    }
    private static bool HasDialog()
    {
        foreach (var name in new[] { "SelectYesno","MiragePrismPrismSetConvert","MiragePrismPrismSetConvertC" })
        { var addon = Plugin.GameGui.GetAddonByName(name); if (!addon.IsNull && addon.IsReady && addon.IsVisible) return true; }
        return false;
    }
}
