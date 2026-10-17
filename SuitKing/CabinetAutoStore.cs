using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;

namespace SuitKing;

internal sealed unsafe class CabinetAutoStore(Plugin plugin)
{
    private readonly Queue<OutfitEquipment> pending = new();
    private OutfitEquipment? current;
    private bool waiting;
    private long deadline;
    private long nextAction;
    private ulong characterId;
    private int completed;
    private int total;
    private int skipped;
    internal bool IsRunning { get; private set; }
    internal string Status { get; private set; } = "";

    internal void Toggle()
    {
        if (IsRunning) { Stop("已停止，已发送的收纳仍可能完成。"); return; }
        try
        {
            if (!plugin.IsCabinetStoreOpen || plugin.Completion.IsRunning || plugin.Quality.IsRunning || plugin.Creation.IsRunning)
            { Status = "请先打开收藏柜的存入界面。"; return; }
            plugin.Inventory.Refresh(false);
            if (!plugin.Inventory.IsComplete || !plugin.Inventory.IsCabinetLoaded)
            { Status = "等待收藏柜、库存和职业配装数据加载。"; return; }
            if (HasConfirmation()) { Status = "请先关闭已有的确认窗口。"; return; }
            pending.Clear();
            foreach (var item in plugin.Inventory.CabinetEquipment.GroupBy(item => item.CabinetId!.Value).Select(group => group.First()))
                pending.Enqueue(item);
            total = pending.Count;
            completed = skipped = 0;
            if (total == 0) { Status = "没有尚未收录且可存入收藏柜的装备。"; return; }
            characterId = Plugin.Player.ContentId;
            waiting = false;
            current = null;
            IsRunning = true;
            deadline = Environment.TickCount64 + 15_000;
            nextAction = 0;
            Status = $"准备存入收藏柜 {total} 件。";
        }
        catch (Exception error)
        {
            Plugin.Log.Error(error, "SuitKing cabinet store could not start");
            Stop("无法开始收藏柜收纳，请查看 /xllog。");
        }
    }

    internal void Stop(string message)
    {
        IsRunning = false;
        pending.Clear();
        current = null;
        Status = $"{message} 已存入 {completed} / {total} 件。";
        plugin.RequestRefresh();
    }

    internal void Tick()
    {
        if (!IsRunning) return;
        try
        {
            if (!plugin.IsCabinetStoreOpen || Plugin.Player.ContentId != characterId)
            { Stop("收藏柜已关闭或角色已切换。"); return; }
            var now = Environment.TickCount64;
            if (now > deadline) { Stop("等待游戏收纳超时，未重复提交。"); return; }
            if (now < nextAction) return;
            nextAction = now + 400;
            var ui = UIState.Instance();
            if (ui == null || !ui->Cabinet.IsCabinetLoaded()) return;
            if (waiting && current != null)
            {
                if (!ui->Cabinet.IsItemInCabinet(current.CabinetId!.Value) || SameSource(current)) return;
                completed++;
                waiting = false;
                current = null;
                deadline = now + 15_000;
                plugin.RequestRefresh();
            }
            if (HasConfirmation()) { Stop("出现确认窗口，已停止自动收纳，请手动检查。"); return; }
            plugin.Inventory.Refresh(false);
            if (!plugin.Inventory.IsComplete || !plugin.Inventory.IsCabinetLoaded) return;
            while (pending.TryDequeue(out var item))
            {
                if (ui->Cabinet.IsItemInCabinet(item.CabinetId!.Value)) { skipped++; continue; }
                var snapshot = plugin.Inventory.CabinetEquipment.FirstOrDefault(entry => entry.CabinetId == item.CabinetId
                    && entry.Location == item.Location && entry.Slot == item.Slot && entry.HighQuality == item.HighQuality);
                if (snapshot == null || snapshot.Condition < 30000 || !CabinetPolicy.CanStore(item.CabinetId, false, snapshot.RegisteredToGearset,
                    plugin.Inventory.CountCabinetCopies(item.ItemId))
                    || !SameSource(item) || IsProtected(item))
                { skipped++; continue; }
                // The native method accepts a Cabinet row ID, not a source slot. Reject duplicate
                // copies so it cannot choose a protected or different copy behind the UI's back.
                current = item;
                waiting = true; // Latch BEFORE sending: no frame retries a storage request.
                deadline = now + 15_000;
                Status = $"正在存入 {completed} / {total} 件：{item.Name}";
                // Store by Cabinet row ID across bags and armory. CabinetStore's visible-page
                // array is not an all-inventory eligibility list and is not required here.
                if (!ui->Cabinet.StoreCabinetItem(item.CabinetId!.Value))
                {
                    Plugin.Log.Warning("SuitKing cabinet rejected {ItemId} / cabinet {CabinetId} from {Location}:{Slot}",
                        item.ItemId, item.CabinetId, item.Location, item.Slot);
                    Stop($"游戏未接受 {item.Name} 的收纳，请检查原生收纳条件。");
                }
                return;
            }
            Stop(skipped > 0 ? $"收纳结束，跳过 {skipped} 件重复副本、已收录或暂不满足条件的装备。" : "收藏柜收纳完成。");
        }
        catch (Exception error)
        {
            Plugin.Log.Error(error, "SuitKing cabinet store stopped");
            Stop("收藏柜收纳发生错误，请查看 /xllog。");
        }
    }

    private static bool SameSource(OutfitEquipment item)
    {
        if (!OutfitInventoryReader.TryResolveContainer(item.Location, out var type)) return false;
        var manager = InventoryManager.Instance();
        if (manager == null) return false;
        var container = manager->GetInventoryContainer(type);
        if (container == null || !container->IsLoaded || item.Slot < 1 || item.Slot > container->Size) return false;
        var slot = container->GetInventorySlot(item.Slot - 1);
        return slot != null && !slot->IsSymbolic && slot->Quantity > 0 && slot->ItemId == item.ItemId
            && ((slot->Flags & InventoryItem.ItemFlags.HighQuality) != 0) == item.HighQuality;
    }

    private static bool IsProtected(OutfitEquipment item)
    {
        var module = RaptureGearsetModule.Instance();
        if (module == null) return true;
        var manager = InventoryManager.Instance();
        if (manager == null) return true;
        var worn = manager->GetInventoryContainer(InventoryType.EquippedItems);
        if (worn == null || !worn->IsLoaded || worn->Size < 0 || worn->Size > 20 || worn->Items == null) return true;
        for (var i = 0; i < worn->Size; i++)
            if (worn->Items[i].ItemId == item.ItemId) return true;
        var rawId = item.ItemId + (item.HighQuality ? 1_000_000u : 0u);
        foreach (ref var set in module->Entries)
            if ((set.Flags & RaptureGearsetModule.GearsetFlag.Exists) != 0)
                foreach (ref var piece in set.Items)
                    if (piece.ItemId == rawId) return true;
        return false;
    }

    private static bool HasConfirmation()
    {
        var agent = AgentCabinet.Instance();
        var yesno = Plugin.GameGui.GetAddonByName("SelectYesno");
        return agent == null || agent->ConfirmationAddonId != 0
            || (!yesno.IsNull && yesno.IsVisible);
    }

}
