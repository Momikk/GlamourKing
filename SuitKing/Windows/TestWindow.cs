using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;

namespace SuitKing.Windows;

internal sealed class TestWindow : Window
{
    private readonly Plugin plugin;

    public TestWindow(Plugin plugin)
        : base("套装之王 · 测试窗口###SuitKingTest")
    {
        this.plugin = plugin;
        Size = new Vector2(440, 230);
        SizeCondition = ImGuiCond.FirstUseEver;
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(360, 200),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue),
        };
    }

    public override void Draw()
    {
        ImGui.TextUnformatted("套装之王基准版本已加载。");
        ImGui.Separator();
        ImGui.TextUnformatted(plugin.IsDresserOpen ? "幻化柜状态：已打开" : "幻化柜状态：未打开");
        ImGui.TextUnformatted($"本次加载期间打开次数：{plugin.DresserOpenCount}");

        if (plugin.LastDresserOpenedAt is { } lastOpened)
            ImGui.TextUnformatted($"最近打开时间：{lastOpened:HH:mm:ss}");

        ImGui.Spacing();
        ImGui.TextWrapped("此窗口用于确认插件加载和幻化柜打开检测是否正常。");
        ImGui.TextUnformatted("手动开关：/suitking 或 /sking");

        if (ImGui.Button("关闭测试窗口"))
            IsOpen = false;
    }
}
