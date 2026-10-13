using Dalamud.Game.Command;
using Dalamud.Interface.Windowing;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using GlamourKing.Windows;

namespace GlamourKing;

public sealed class Plugin : IDalamudPlugin
{
    public const string DresserAddonName = "MiragePrismPrismBox";
    private const string CommandName = "/suitking";

    [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
    [PluginService] internal static ICommandManager CommandManager { get; private set; } = null!;
    [PluginService] internal static IFramework Framework { get; private set; } = null!;
    [PluginService] internal static IGameGui GameGui { get; private set; } = null!;
    [PluginService] internal static IPluginLog Log { get; private set; } = null!;

    private readonly WindowSystem windowSystem = new("GlamourKing");
    private readonly TestWindow testWindow;

    public bool IsDresserOpen { get; private set; }
    public int DresserOpenCount { get; private set; }
    public DateTimeOffset? LastDresserOpenedAt { get; private set; }

    public Plugin()
    {
        testWindow = new TestWindow(this);
        windowSystem.AddWindow(testWindow);

        CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
        {
            HelpMessage = "打开或关闭套装之王测试窗口。",
        });

        CommandManager.AddHandler("/sking", new CommandInfo(OnCommand) { HelpMessage = "打开或关闭套装之王测试窗口。" });

        PluginInterface.UiBuilder.Draw += windowSystem.Draw;
        PluginInterface.UiBuilder.OpenMainUi += OpenMainUi;
        Framework.Update += OnFrameworkUpdate;

        Log.Information("SuitKing baseline loaded (API {ApiLevel}).", PluginInterface.Manifest.DalamudApiLevel);
    }

    public void Dispose()
    {
        Framework.Update -= OnFrameworkUpdate;
        PluginInterface.UiBuilder.Draw -= windowSystem.Draw;
        PluginInterface.UiBuilder.OpenMainUi -= OpenMainUi;
        CommandManager.RemoveHandler(CommandName);
        CommandManager.RemoveHandler("/sking");
        windowSystem.RemoveAllWindows();
        Log.Information("SuitKing baseline unloaded.");
    }

    private void OnFrameworkUpdate(IFramework framework)
    {
        // Reacquire the public wrapper each tick; never retain a game UI pointer.
        // Polling also handles hot-loading while the dresser is already open and
        // visibility changes that do not create a new addon instance.
        var addon = GameGui.GetAddonByName(DresserAddonName);
        var isOpen = !addon.IsNull && addon.IsReady && addon.IsVisible;

        if (isOpen == IsDresserOpen)
            return;

        IsDresserOpen = isOpen;

        if (isOpen)
        {
            DresserOpenCount++;
            LastDresserOpenedAt = DateTimeOffset.Now;
            testWindow.IsOpen = true;
            Log.Information("Glamour dresser opened; showing test UI (#{OpenCount}).", DresserOpenCount);
        }
        else
        {
            testWindow.IsOpen = false;
            Log.Information("Glamour dresser closed; hiding test UI.");
        }
    }

    private void OnCommand(string command, string args) => testWindow.Toggle();

    private void OpenMainUi() => testWindow.IsOpen = true;
}
