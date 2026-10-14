using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Textures;
using Dalamud.Interface.Windowing;

namespace SuitKing.Windows;

internal sealed class MainWindow : Window
{
    private readonly Plugin plugin;
    private OutfitStorageState? selectedState = OutfitStorageState.CanComplete;
    private bool selectDefaultTab = true;

    public MainWindow(Plugin plugin) : base("套装之王 SuitKing###SuitKingMain")
    {
        this.plugin = plugin;
        Size = new Vector2(850, 560);
        SizeCondition = ImGuiCond.FirstUseEver;
        SizeConstraints = new() { MinimumSize = new Vector2(620, 300), MaximumSize = new Vector2(float.MaxValue) };
    }

    internal void OpenOnCompletable()
    {
        selectedState = OutfitStorageState.CanComplete;
        selectDefaultTab = true;
        IsOpen = true;
    }

    public override void Draw()
    {
        var hide = plugin.Config.HideGearsetItems;
        var left = ImGui.GetCursorPosX();
        var width = ImGui.CalcTextSize("隐藏套装中的物品").X + ImGui.GetFrameHeight() + ImGui.GetStyle().ItemInnerSpacing.X;
        ImGui.SetCursorPosX(left + Math.Max(0, ImGui.GetContentRegionAvail().X - width));
        if (ImGui.Checkbox("隐藏套装中的物品", ref hide))
        {
            plugin.Config.HideGearsetItems = hide;
            plugin.SaveConfiguration();
        }
        ImGui.SetCursorPosX(left);
        var inventory = plugin.Inventory;
        var equipment = OutfitDisplayFilter.Apply(inventory.Equipment, hide, "");
        if (ImGui.BeginTabBar("OutfitStatusTabs"))
        {
            DrawTab("全部", null, equipment);
            DrawTab("已保存", OutfitStorageState.Saved, equipment);
            DrawTab("可补齐", OutfitStorageState.CanComplete, equipment);
            DrawTab("未建立", OutfitStorageState.NotCreated, equipment);
            if (equipment.Any(item => item.Assessment?.State is null or OutfitStorageState.Unknown))
                DrawTab("待确认", OutfitStorageState.Unknown, equipment);
            selectDefaultTab = false;
            ImGui.EndTabBar();
        }
        if (!plugin.IsDresserOpen)
        {
            ImGui.TextUnformatted("请打开投影台以查看装备。");
            return;
        }
        if (!inventory.IsComplete || !inventory.AreOutfitsLoaded)
            ImGui.TextWrapped(inventory.IsComplete ? "投影台套装数据加载中。" : inventory.Status);
        var matches = equipment.Where(item => selectedState is null || item.Assessment?.State == selectedState)
            .OrderBy(item => EquipmentParts.Rank(item.Part)).ThenBy(item => item.Name, StringComparer.Ordinal).ToArray();
        if (matches.Length == 0)
        {
            ImGui.TextUnformatted(inventory.IsComplete && inventory.AreOutfitsLoaded ? "这个类别暂时没有装备。" : "等待数据加载。");
            return;
        }
        var hidden = hide ? inventory.Equipment.Count(item => item.RegisteredToGearset == true) : 0;
        var footerHeight = ImGui.GetTextLineHeightWithSpacing() + ImGui.GetStyle().ItemSpacing.Y;
        if (ImGui.BeginTable("EquipmentByPart", 5, ImGuiTableFlags.RowBg | ImGuiTableFlags.BordersInnerH
            | ImGuiTableFlags.ScrollY | ImGuiTableFlags.Resizable, new Vector2(0, -footerHeight)))
        {
            try
            {
                ImGui.TableSetupScrollFreeze(0, 1);
                ImGui.TableSetupColumn("图标", ImGuiTableColumnFlags.WidthFixed, 32);
                ImGui.TableSetupColumn("装备名称", ImGuiTableColumnFlags.WidthStretch, 2.2f);
                ImGui.TableSetupColumn("部位", ImGuiTableColumnFlags.WidthFixed, 40);
                ImGui.TableSetupColumn("所属套装", ImGuiTableColumnFlags.WidthStretch, 2);
                ImGui.TableSetupColumn("套装状态", ImGuiTableColumnFlags.WidthFixed, 75);
                ImGui.TableHeadersRow();
                string? group = null;
                foreach (var item in matches)
                {
                    var itemGroup = EquipmentParts.Group(item.Part);
                    if (itemGroup != group)
                    {
                        group = itemGroup;
                        ImGui.TableNextRow();
                        ImGui.TableSetBgColor(ImGuiTableBgTarget.RowBg0, ImGui.GetColorU32(ImGuiCol.Header));
                        ImGui.TableSetColumnIndex(1);
                        ImGui.TextUnformatted(group);
                    }
                    ImGui.TableNextRow();
                    ImGui.TableSetColumnIndex(0);
                    var icon = item.IconId == 0 ? null : Plugin.Textures.GetFromGameIcon(new GameIconLookup(item.IconId)).GetWrapOrDefault();
                    if (icon is not null) ImGui.Image(icon.Handle, new Vector2(28));
                    else ImGui.Dummy(new Vector2(28));
                    ImGui.TableNextColumn();
                    ImGui.TextUnformatted(item.Name);
                    if (ImGui.IsItemHovered()) ImGui.SetTooltip($"{item.Location} · 第 {item.Slot} 格 · {(item.HighQuality ? "HQ" : "NQ")}");
                    ImGui.TableNextColumn();
                    ImGui.TextUnformatted(item.Part);
                    ImGui.TableNextColumn();
                    ImGui.TextWrapped(item.SetNames);
                    ImGui.TableNextColumn();
                    var state = item.Assessment?.State ?? OutfitStorageState.Unknown;
                    var (label, color) = state switch
                    {
                        OutfitStorageState.Saved => ("已保存", new Vector4(.55f, .85f, .55f, 1)),
                        OutfitStorageState.CanComplete => ("可补齐", new Vector4(1, .8f, .3f, 1)),
                        OutfitStorageState.NotCreated => ("未建立", new Vector4(.5f, .75f, 1, 1)),
                        _ => ("待确认", new Vector4(.65f, .65f, .65f, 1)),
                    };
                    ImGui.TextColored(color, label);
                    if (ImGui.IsItemHovered()) ImGui.SetTooltip(item.Assessment?.Details ?? "等待套装数据。");
                }
            }
            finally { ImGui.EndTable(); }
        }
        ImGui.TextUnformatted($"显示 {matches.Length} 件" + (hide ? $" · 已隐藏 {hidden} 件" : ""));
    }

    private void DrawTab(string label, OutfitStorageState? state, IReadOnlyList<OutfitEquipment> equipment)
    {
        var count = equipment.Count(item => state is null || item.Assessment?.State == state);
        var flags = selectDefaultTab && state == OutfitStorageState.CanComplete ? ImGuiTabItemFlags.SetSelected : ImGuiTabItemFlags.None;
        if (ImGui.BeginTabItem($"{label} {count}###{label}", flags))
        {
            if (!selectDefaultTab || state == OutfitStorageState.CanComplete) selectedState = state;
            ImGui.EndTabItem();
        }
    }
}
