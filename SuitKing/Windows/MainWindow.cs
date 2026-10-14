using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;

namespace SuitKing.Windows;

internal sealed class MainWindow : Window
{
    private readonly Plugin plugin;
    private string search = "";

    public MainWindow(Plugin plugin) : base("套装之王 SuitKing###SuitKingMain")
    {
        this.plugin = plugin;
        Size = new Vector2(850, 520);
        SizeCondition = ImGuiCond.FirstUseEver;
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(560, 320),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue),
        };
    }

    public override void Draw()
    {
        var inventory = plugin.Inventory;
        ImGui.TextUnformatted("套装幻影化装备 · 全部分页汇总");
        ImGui.TextWrapped("范围：物品栏与兵装库，不含身上装备、雇员、陆行鸟鞍囊或投影台内已存装备。支持套装幻影化不代表已集齐整套，收纳条件以游戏为准。");
        ImGui.Separator();
        ImGui.TextWrapped(inventory.Status);
        if (!plugin.IsDresserOpen)
        {
            ImGui.TextUnformatted("请打开投影台以读取装备。");
            return;
        }
        if (inventory.UpdatedAt is { } updated)
            ImGui.TextUnformatted($"最近刷新：{updated:HH:mm:ss} · 每秒自动刷新");
        if (ImGui.Button("立即刷新"))
            plugin.RequestRefresh();
        ImGui.SameLine();
        ImGui.SetNextItemWidth(300);
        ImGui.InputTextWithHint("##Search", "搜索装备或套装名称", ref search, 128);
        var hideGearsets = plugin.Config.HideGearsetItems;
        if (ImGui.Checkbox("隐藏套装中的物品", ref hideGearsets))
        {
            plugin.Config.HideGearsetItems = hideGearsets;
            plugin.SaveConfiguration();
        }
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip("隐藏已登记到装备套装（职业配装）中的物品，与游戏的同名过滤作用相同。\n该选项独立保存，不改变游戏窗口的勾选状态。");
        var matches = OutfitDisplayFilter.Apply(inventory.Equipment, hideGearsets, search);
        var registered = inventory.Equipment.Count(item => item.RegisteredToGearset == true);
        ImGui.TextUnformatted($"显示 {matches.Count} / {inventory.Equipment.Count} 件 · 套装过滤隐藏 {(hideGearsets ? registered : 0)} 件");
        if (hideGearsets && inventory.Equipment.Any(item => item.RegisteredToGearset is null))
            ImGui.TextWrapped("装备套装数据尚未就绪，未确认的物品暂时保留显示。");
        if (matches.Count == 0)
        {
            ImGui.TextWrapped(inventory.IsComplete
                ? (inventory.Equipment.Count == 0 ? "当前物品栏与兵装库没有支持套装幻影化的装备。" : "没有符合当前过滤条件的装备。")
                : "数据尚未完整加载，请稍候。");
            return;
        }
        if (!ImGui.BeginTable("OutfitEquipment", 4,
            ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.ScrollY | ImGuiTableFlags.Resizable,
            new Vector2(0, -1)))
            return;
        try
        {
            ImGui.TableSetupScrollFreeze(0, 1);
            ImGui.TableSetupColumn("装备");
            ImGui.TableSetupColumn("品质");
            ImGui.TableSetupColumn("位置");
            ImGui.TableSetupColumn("所属套装");
            ImGui.TableHeadersRow();
            foreach (var item in matches)
            {
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGui.TextUnformatted(item.Name);
                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip($"物品 ID：{item.ItemId}\n耐久：{item.Condition / 300f:0.#}%");
                ImGui.TableNextColumn();
                ImGui.TextUnformatted(item.HighQuality ? "HQ" : "NQ");
                ImGui.TableNextColumn();
                ImGui.TextUnformatted($"{item.Location} · 第 {item.Slot} 格");
                ImGui.TableNextColumn();
                ImGui.TextWrapped(item.SetNames);
            }
        }
        finally { ImGui.EndTable(); }
    }
}
