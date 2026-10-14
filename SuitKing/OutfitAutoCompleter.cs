using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Lumina.Excel.Sheets;

namespace SuitKing;

internal sealed unsafe class OutfitAutoCompleter(Plugin plugin)
{
    private const string ConvertName = "MiragePrismPrismSetConvert";
    private const string ConfirmName = "MiragePrismPrismSetConvertC";
    private const string SelectorName = "MiragePrismPrismBoxCrystallize";
    private enum Phase { Idle, Next, Confirm, Convert, Result }
    private Phase phase;
    private readonly Queue<(CompletionItemKey Key, string Name)> pending = new();
    private readonly HashSet<CompletionItemKey> allowed = [];
    private readonly HashSet<CompletionItemKey> completedKeys = [];
    private CompletionItemKey source;
    private readonly List<(CompletionItemKey Key, int OutfitSlot)> submitted = [];
    private uint prismBoxIndex;
    private uint outfitId;
    private ulong characterId;
    private bool hideGearsets;
    private int completed;
    private int total;
    private int skipped;
    private long deadline;
    private long nextAction;
    private string targetName = "";
    internal bool IsRunning => phase != Phase.Idle;
    internal string Status { get; private set; } = "";

    internal void Toggle()
    {
        try { ToggleCore(); }
        catch (Exception exception)
        {
            Plugin.Log.Error(exception, "SuitKing could not start auto-completion");
            if (IsRunning) Stop("无法开始补齐，请查看 /xllog。");
            else Status = "无法开始补齐，请查看 /xllog。";
        }
    }

    private void ToggleCore()
    {
        if (IsRunning) { Stop("已停止；已提交的操作仍可能完成。"); return; }
        if (!plugin.IsDresserOpen || Plugin.Player.ContentId == 0)
        { Status = "请先打开投影台。"; return; }
        if (GetAddon(ConvertName) != null || GetAddon(ConfirmName) != null || GetAddon("SelectYesno") != null)
        { Status = "请先关闭已有的确认或套装幻影化窗口。"; return; }
        plugin.Inventory.Refresh();
        if (!plugin.Inventory.IsComplete || !plugin.Inventory.AreOutfitsLoaded)
        { Status = "等待物品栏和投影台套装数据加载。"; return; }
        pending.Clear(); allowed.Clear(); submitted.Clear(); completedKeys.Clear();
        completed = skipped = 0;
        hideGearsets = plugin.Config.HideGearsetItems;
        foreach (var item in OutfitDisplayFilter.Apply(plugin.Inventory.Equipment, hideGearsets, ""))
        {
            if (item.Assessment?.State != OutfitStorageState.CanComplete) continue;
            if (item.Condition < 30000) { skipped++; continue; }
            if (!OutfitInventoryReader.TryResolveContainer(item.Location, out var container)
                || item.Slot < 1 || item.Slot > ushort.MaxValue) continue;
            var key = new CompletionItemKey(item.ItemId, (uint)container, (ushort)(item.Slot - 1), item.HighQuality);
            if (allowed.Add(key)) pending.Enqueue((key, item.Name));
        }
        total = pending.Count;
        if (total == 0)
        { Status = skipped > 0 ? $"有 {skipped} 件装备需要修理，暂时没有可提交装备。" : "没有可补齐的装备。"; return; }
        characterId = Plugin.Player.ContentId;
        SetPhase(Phase.Next);
        Status = $"准备自动补齐 {total} 件" + (skipped > 0 ? $"，跳过 {skipped} 件需修理装备。" : "。");
    }

    internal void Stop(string message)
    {
        phase = Phase.Idle;
        pending.Clear(); allowed.Clear(); submitted.Clear(); completedKeys.Clear();
        Status = $"{message} 已补齐 {completed} / {total} 件。";
        plugin.RequestRefresh();
    }

    internal void Tick()
    {
        if (!IsRunning) return;
        try
        {
            if (!plugin.IsDresserOpen || Plugin.Player.ContentId != characterId)
            { Stop("投影台已关闭或角色已切换。"); return; }
            if (plugin.Config.HideGearsetItems != hideGearsets)
            { Stop("过滤条件已改变。"); return; }
            var manager = MirageManager.Instance();
            if (manager == null || !manager->PrismBoxLoaded)
            { Stop("套装数据未就绪。"); return; }
            var now = Environment.TickCount64;
            if (now > deadline) { Stop("等待游戏响应超时，未重试提交。"); return; }
            if (now < nextAction) return;
            nextAction = now + 200;
            switch (phase)
            {
                case Phase.Next: OpenNext(); break;
                case Phase.Confirm: ConfirmMerge(); break;
                case Phase.Convert: SubmitConversion(); break;
                case Phase.Result: WaitForResult(manager); break;
            }
        }
        catch (Exception exception)
        {
            Plugin.Log.Error(exception, "SuitKing auto-completion stopped in {Phase}", phase);
            Stop(exception is InvalidOperationException ? exception.Message : "自动补齐遇到错误，请查看 /xllog。");
        }
    }

    private void OpenNext()
    {
        if (GetAddon(ConvertName) != null || GetAddon(ConfirmName) != null || GetAddon("SelectYesno") != null)
        { Stop("出现了其他或尚未关闭的窗口。"); return; }
        plugin.Inventory.Refresh();
        while (pending.TryDequeue(out var target))
        {
            if (completedKeys.Contains(target.Key)) continue;
            var item = plugin.Inventory.Equipment.FirstOrDefault(item => KeyOf(item) == target.Key);
            if (item is null || item.Assessment?.State != OutfitStorageState.CanComplete)
            { skipped++; continue; }
            if (!IsCurrentEligible(target.Key)) { Stop("目标装备已改变或不满足收纳条件。"); return; }
            source = target.Key;
            targetName = target.Name;
            var dresser = GetAddon(Plugin.DresserAddonName);
            var selector = GetAddon(SelectorName);
            if (dresser == null) { Stop("请保持投影台主窗口打开。"); return; }
            var agent = AgentMiragePrismPrismSetConvert.Instance();
            if (agent == null) { Stop("套装幻影化接口未就绪。"); return; }
            SetPhase(Phase.Confirm);
            Status = $"正在补齐 {completed} / {total} 件：{targetName}";
            // Same native entry point as the outfit icon, without changing the inventory page.
            if (!agent->Open(source.ItemId, (InventoryType)source.Container, source.Slot,
                    selector == null ? (ushort)0 : selector->Id, dresser->Id, true))
                Stop("游戏未能打开目标装备的套装幻影化。");
            return;
        }
        Stop(skipped > 0 ? $"本次处理结束，跳过 {skipped} 件已变化或需修理装备。" : "自动补齐完成。");
    }

    private AgentMiragePrismPrismSetConvert.AgentData* OwnedData()
    {
        var agent = AgentMiragePrismPrismSetConvert.Instance();
        if (agent == null || agent->Data == null) return null;
        var data = agent->Data;
        if (data->SourceItemId % 1_000_000 != source.ItemId || (uint)data->SourceContainer != source.Container
            || data->SourceSlot != source.Slot || !data->EnableStoring || !IsCurrentEligible(source))
            throw new InvalidOperationException("套装窗口不再属于本次装备。");
        // Never create a new outfit. Re-read the actual existing dresser record selected by the game.
        if (!data->StoreInExistingOutfit) throw new InvalidOperationException("游戏未选择补齐已有套装。");
        return data;
    }

    private void ConfirmMerge()
    {
        var data = OwnedData();
        if (data == null) return;
        var confirm = GetAddon(ConfirmName);
        var yesno = (AddonSelectYesno*)GetAddon("SelectYesno");
        if (confirm != null)
        {
            if (data->ConfirmationAddonId != confirm->Id)
            { Stop("套装确认窗口不属于本次操作。"); return; }
            if (!ClickButton(confirm, ((AddonMiragePrismPrismSetConvertC*)confirm)->YesButton)) return;
            SetPhase(Phase.Convert);
            return;
        }
        if (yesno != null)
        {
            var box = AgentMiragePrismPrismBox.Instance();
            var ownedId = data->ConfirmationAddonId == yesno->Id
                || (box != null && box->ContextAddonId == yesno->Id);
            var prompt = yesno->PromptText == null ? "" : yesno->PromptText->NodeText.ToString();
            if (!ownedId || !OutfitCompletionPolicy.IsMergePrompt(prompt))
            { Stop("出现了不匹配的确认提示，未自动确认。"); return; }
            if (!ClickButton((AtkUnitBase*)yesno, yesno->YesButton)) return;
            SetPhase(Phase.Convert);
            return;
        }
        // Another plugin may have already confirmed this owned dialog; still validate its hand-ins.
        if (GetAddon(ConvertName) != null) SetPhase(Phase.Convert);
    }

    private void SubmitConversion()
    {
        if (GetAddon(ConfirmName) != null || GetAddon("SelectYesno") != null)
        { Stop("确认窗口尚未关闭，未提交。"); return; }
        var addon = (AddonMiragePrismPrismSetConvert*)GetAddon(ConvertName);
        var data = OwnedData();
        if (addon == null || data == null || data->State != SetConvertAddonState.Ready) return;
        var manager = MirageManager.Instance();
        if (data->PrismBoxIndex >= 800 || data->ItemSetCount == 0 || data->ItemSetCount > 5
            || data->SelectedSetIndex >= data->ItemSetCount || data->NumItemsInSet == 0 || data->NumItemsInSet > 9)
            throw new InvalidOperationException("套装数据结构异常。");
        prismBoxIndex = data->PrismBoxIndex;
        outfitId = manager->PrismBoxItemIds[(int)prismBoxIndex] % 1_000_000;
        if (data->ItemSets[(int)data->SelectedSetIndex].ItemId != outfitId
            || !Plugin.Data.GetExcelSheet<MirageStoreSetItem>().TryGetRow(outfitId, out var set))
            throw new InvalidOperationException("选中的套装与已有记录不一致。");
        plugin.Inventory.Refresh();
        var pieces = OutfitCatalog.PiecesInOrder(set);
        submitted.Clear();
        if (addon->AtkValues == null || addon->AtkValuesCount < 21 + data->NumItemsInSet * 7) return;
        for (var i = 0; i < data->NumItemsInSet; i++)
        {
            var offset = 21 + i * 7;
            if (addon->AtkValues[offset + 6].UInt != 3) continue; // native Filled flag
            var nativeItem = data->Items[i];
            if (nativeItem.InventoryType == InventoryType.Invalid || nativeItem.Slot > ushort.MaxValue)
                throw new InvalidOperationException("提交位置无效。");
            var key = ReadKey(nativeItem.InventoryType, (ushort)nativeItem.Slot);
            if (key is null || key.Value.ItemId != nativeItem.ItemId % 1_000_000)
                throw new InvalidOperationException("提交装备已经改变。");
            if (addon->AtkValues[offset].UInt % 1_000_000 != key.Value.ItemId
                || addon->AtkValues[offset + 4].UInt != key.Value.Container
                || addon->AtkValues[offset + 5].UInt != key.Value.Slot)
                throw new InvalidOperationException("原生提交窗口与目标格子不一致。");
            var slot = Array.IndexOf(pieces, key.Value.ItemId);
            if (slot < 0 || manager->IsSetSlotUnlocked(prismBoxIndex, slot) || !IsCurrentEligible(key.Value))
                throw new InvalidOperationException("提交装备不是当前套装缺少的部件。");
            submitted.Add((key.Value, slot));
        }
        if (submitted.Select(item => item.OutfitSlot).Distinct().Count() != submitted.Count)
            throw new InvalidOperationException("同一套装部位被重复选择，未提交。");
        var reason = OutfitCompletionPolicy.ValidateHandIns(allowed, submitted.Select(item => item.Key).ToArray(),
            source, addon->AtkValues[1].UInt);
        if (reason != null) { Stop(reason); return; }
        var button = addon->StoreAsGlamourButton;
        if (button == null || !button->IsEnabled) { Stop("幻影化按钮不可用，请检查品质、耐久或材料。"); return; }
        // Move out of Submit BEFORE sending the click: no frame can submit a second time.
        SetPhase(Phase.Result);
        if (!ClickButton((AtkUnitBase*)addon, button)) Stop("无法识别幻影化按钮事件，未提交。");
    }

    private void WaitForResult(MirageManager* manager)
    {
        if (manager->PrismBoxItemIds[(int)prismBoxIndex] % 1_000_000 != outfitId)
        { Stop("目标套装记录已改变。"); return; }
        if (submitted.Any(item => !manager->IsSetSlotUnlocked(prismBoxIndex, item.OutfitSlot)
            || ReadKey((InventoryType)item.Key.Container, item.Key.Slot) == item.Key)) return;
        if (GetAddon(ConvertName) != null || GetAddon(ConfirmName) != null || GetAddon("SelectYesno") != null) return;
        foreach (var item in submitted) completedKeys.Add(item.Key);
        completed += submitted.Count;
        submitted.Clear();
        plugin.RequestRefresh();
        SetPhase(Phase.Next);
    }

    private bool IsCurrentEligible(CompletionItemKey key)
    {
        if (ReadKey((InventoryType)key.Container, key.Slot) != key) return false;
        var manager = InventoryManager.Instance();
        var item = manager->GetInventoryContainer((InventoryType)key.Container)->GetInventorySlot(key.Slot);
        var gearsets = RaptureGearsetModule.Instance();
        return item->Condition >= 30000 && (!hideGearsets || (gearsets != null && !gearsets->IsItemRegisteredToGearset(item)))
            && plugin.Inventory.Equipment.Any(entry => KeyOf(entry) == key
                && entry.Assessment?.State == OutfitStorageState.CanComplete
                && (!hideGearsets || entry.RegisteredToGearset != true));
    }

    private static CompletionItemKey? KeyOf(OutfitEquipment item)
        => OutfitInventoryReader.TryResolveContainer(item.Location, out var container) && item.Slot > 0
            ? new(item.ItemId, (uint)container, (ushort)(item.Slot - 1), item.HighQuality) : null;

    private static CompletionItemKey? ReadKey(InventoryType container, ushort slot)
    {
        var manager = InventoryManager.Instance();
        if (manager == null) return null;
        var inventory = manager->GetInventoryContainer(container);
        if (inventory == null || !inventory->IsLoaded || slot >= inventory->Size) return null;
        var item = inventory->GetInventorySlot(slot);
        if (item == null || item->IsSymbolic || item->ItemId == 0 || item->Quantity == 0) return null;
        return new(item->ItemId, (uint)container, slot, (item->Flags & InventoryItem.ItemFlags.HighQuality) != 0);
    }

    private static AtkUnitBase* GetAddon(string name)
    {
        var handle = Plugin.GameGui.GetAddonByName(name);
        return !handle.IsNull && handle.IsReady && handle.IsVisible ? (AtkUnitBase*)handle.Address : null;
    }

    private static bool ClickButton(AtkUnitBase* addon, AtkComponentButton* button)
    {
        if (button == null || !button->IsEnabled || button->OwnerNode == null || !button->AtkResNode->IsVisible()) return false;
        // Use the real registered event, not a guessed FireCallback argument or a signature hook.
        var evt = button->OwnerNode->AtkResNode.AtkEventManager.Event;
        for (var i = 0; evt != null && i < 64; i++, evt = evt->NextEvent)
        {
            if (evt->State.EventType != AtkEventType.ButtonClick || evt->Listener != (AtkEventListener*)addon) continue;
            if ((evt->State.StateFlags & AtkEventStateFlags.IsGlobalEvent) != 0)
                addon->ReceiveGlobalEvent(evt->State.EventType, (int)evt->Param, evt);
            else addon->ReceiveEvent(evt->State.EventType, (int)evt->Param, evt);
            return true;
        }
        return false;
    }

    private void SetPhase(Phase next)
    {
        phase = next;
        deadline = Environment.TickCount64 + 15_000;
        nextAction = Environment.TickCount64 + 300;
    }
}