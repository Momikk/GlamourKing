using System.Numerics;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.BaseTypes;
using KamiToolKit.Nodes;
using Lumina.Data.Parsing.Uld;

namespace SuitKing.Windows;

internal sealed unsafe class NativeMainWindow(Plugin plugin) : NativeAddon
{
    private CheckboxNode? hideCheckbox;
    private TabBarNode? tabs;
    private ListNode<NativeEquipmentRow, NativeEquipmentRowNode>? list;
    private TextNode? footer;
    private ResNode? header;
    private bool pendingOpen;
    private OutfitStorageState? selectedState = OutfitStorageState.CanComplete;
    private IReadOnlyList<OutfitEquipment>? lastEquipment;
    private bool lastHide;
    private bool dirty = true;
    private static readonly (string Label, OutfitStorageState? State)[] Categories =
    [ ("全部", null), ("已保存", OutfitStorageState.Saved), ("可补齐", OutfitStorageState.CanComplete),
      ("未建立", OutfitStorageState.NotCreated), ("待确认", OutfitStorageState.Unknown) ];

    internal void OpenOnCompletable()
    {
        selectedState = OutfitStorageState.CanComplete;
        dirty = true;
        pendingOpen = !IsOpen;
        TickPendingOpen();
        if (tabs is not null) SelectDefaultTab();
    }

    internal void TickPendingOpen()
    {
        if (!pendingOpen || IsAllocated) return;
        pendingOpen = false;
        Open();
    }

    public override void Close()
    {
        pendingOpen = false;
        base.Close();
    }

    protected override void OnSetup(AtkUnitBase* addon, Span<AtkValue> values)
    {
        var origin = ContentStartPosition;
        var width = ContentSize.X;
        hideCheckbox = new CheckboxNode
        {
            String = "隐藏套装中的物品", IsChecked = plugin.Config.HideGearsetItems,
            Position = origin, Size = new Vector2(250, 24),
            OnClick = value =>
            {
                plugin.Config.HideGearsetItems = value;
                plugin.SaveConfiguration();
                dirty = true;
            }
        };
        AddNode(hideCheckbox);
        tabs = new TabBarNode { Position = origin + new Vector2(0, 29), Size = new Vector2(width, 28) };
        AddNode(tabs);
        foreach (var (label, state) in Categories)
            tabs.AddTab(label, () => { selectedState = state; dirty = true; list?.ResetScroll(); });
        SelectDefaultTab();

        header = new ResNode { Position = origin + new Vector2(0, 62), Size = new Vector2(width - 24, 24) };
        AddNode(header);
        var labels = new[] { "图标", "装备名称", "部位", "所属套装", "套装状态" };
        for (var i = 0; i < labels.Length; i++)
        {
            var (x, cellWidth) = NativeEquipmentRowNode.Column(i, header.Width);
            var text = new TextNode
            {
                String = labels[i], Position = new Vector2(x, 0), Size = new Vector2(cellWidth, 24),
                FontSize = 13, AlignmentType = AlignmentType.Left,
            };
            text.AttachNode(header);
        }
        list = new ListNode<NativeEquipmentRow, NativeEquipmentRowNode>
        {
            OptionsList = [], AutoResetScroll = false, ItemSpacing = 0,
            Position = origin + new Vector2(0, 88), Size = new Vector2(width, ContentSize.Y - 120),
        };
        AddNode(list);
        footer = new TextNode
        {
            Position = origin + new Vector2(0, ContentSize.Y - 24), Size = new Vector2(width, 24),
            FontSize = 12, AlignmentType = AlignmentType.Left,
        };
        AddNode(footer);
        dirty = true;
        RefreshContents();
    }

    private void SelectDefaultTab()
    {
        if (tabs is null) return;
        for (var i = 0; i < tabs.TabButtons.Count; i++)
        {
            tabs.TabButtons[i].IsChecked = i == 2;
            tabs.TabButtons[i].IsSelected = i == 2;
        }
    }

    protected override void OnUpdate(AtkUnitBase* addon)
    {
        if (!IsOpen || list is null) return;
        if (dirty || !ReferenceEquals(lastEquipment, plugin.Inventory.Equipment)
            || lastHide != plugin.Config.HideGearsetItems)
            RefreshContents();
    }

    private void RefreshContents()
    {
        if (list is null || tabs is null || footer is null) return;
        var inventory = plugin.Inventory;
        var hide = plugin.Config.HideGearsetItems;
        var equipment = OutfitDisplayFilter.Apply(inventory.Equipment, hide, "");
        for (var i = 0; i < Categories.Length; i++)
        {
            var (label, state) = Categories[i];
            var count = equipment.Count(item => Matches(item, state));
            tabs.TabButtons[i].String = $"{label} {count}";
        }
        var matches = equipment.Where(item => Matches(item, selectedState))
            .OrderBy(item => EquipmentParts.Rank(item.Part)).ThenBy(item => item.Name, StringComparer.Ordinal)
            .ThenBy(item => item.Location, StringComparer.Ordinal).ThenBy(item => item.Slot).ToArray();
        var rows = new List<NativeEquipmentRow>();
        string? group = null;
        foreach (var item in matches)
        {
            var nextGroup = EquipmentParts.Group(item.Part);
            if (group != nextGroup)
            {
                group = nextGroup;
                rows.Add(new(group, null));
            }
            rows.Add(new(null, item));
        }
        // Keep the scroll position and reuse native row nodes on the one-second data refresh.
        if (!list.OptionsList.SequenceEqual(rows)) list.OptionsList = rows;
        list.NoResultsTextNode.String = !plugin.IsDresserOpen ? "请打开投影台以查看装备。"
            : inventory.IsComplete && inventory.AreOutfitsLoaded ? "这个类别暂时没有装备。" : "等待套装数据加载。";
        var hidden = hide ? inventory.Equipment.Count(item => item.RegisteredToGearset == true) : 0;
        footer.String = inventory.IsComplete && inventory.AreOutfitsLoaded
            ? $"显示 {matches.Length} 件" + (hide ? $" · 已隐藏 {hidden} 件" : "")
            : plugin.IsDresserOpen ? inventory.Status : "请打开投影台以查看装备。";
        lastEquipment = inventory.Equipment;
        lastHide = hide;
        dirty = false;
    }

    private static bool Matches(OutfitEquipment item, OutfitStorageState? state)
        => state is null || (item.Assessment?.State ?? OutfitStorageState.Unknown) == state;

    protected override void OnFinalize(AtkUnitBase* addon)
    {
        // Detach owned children before the game's finalizer; never keep native pointers across reopening.
        hideCheckbox?.Dispose();
        tabs?.Dispose();
        list?.Dispose();
        header?.Dispose();
        footer?.Dispose();
        header = null;
        hideCheckbox = null;
        tabs = null;
        list = null;
        footer = null;
        lastEquipment = null;
    }
}