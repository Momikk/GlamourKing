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
        var matches = inventory.Equipment.Where(item => string.IsNullOrWhiteSpace(search)
            || item.Name.Contains(search, StringComparison.OrdinalIgnoreCase)
            || item.SetNames.Contains(search, StringComparison.OrdinalIgnoreCase)).ToArray();
        ImGui.TextUnformatted($"显示 {matches.Length} / {inventory.Equipment.Count} 件");
        if (matches.Length == 0)
        {
            ImGui.TextWrapped(inventory.IsComplete
                ? (search.Length == 0 ? "当前物品栏与兵装库没有支持套装幻影化的装备。" : "没有匹配搜索条件的装备。")
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
