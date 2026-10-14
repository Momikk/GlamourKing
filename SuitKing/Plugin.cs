using Dalamud.Game.Command;
using Dalamud.Interface.Windowing;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using SuitKing.Windows;

namespace SuitKing;

public sealed class Plugin : IDalamudPlugin
{
    public const string DresserAddonName = "MiragePrismPrismBox";
    private const string SelectionAddonName = "MiragePrismPrismBoxCrystallize";
    private const string CommandName = "/suitking";

    [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
    [PluginService] internal static ICommandManager CommandManager { get; private set; } = null!;
    [PluginService] internal static IFramework Framework { get; private set; } = null!;
    [PluginService] internal static IGameGui GameGui { get; private set; } = null!;
    [PluginService] internal static IPluginLog Log { get; private set; } = null!;
    [PluginService] internal static ITextureProvider Textures { get; private set; } = null!;
    [PluginService] internal static IAddonLifecycle Addons { get; private set; } = null!;
    [PluginService] internal static IDataManager Data { get; private set; } = null!;
    [PluginService] internal static IPlayerState Player { get; private set; } = null!;

    private readonly WindowSystem windowSystem = new("SuitKing");
    private readonly MainWindow mainWindow;
    private readonly NativeEquipmentHighlighter highlighter;
    private long nextScan;
    private ulong characterId;
    private bool selectionWasOpen;
    private string? lastError;

    internal OutfitInventoryReader Inventory { get; } = new();
    internal Configuration Config { get; }
    public bool IsDresserOpen { get; private set; }

    public Plugin()
    {
        Config = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
        mainWindow = new MainWindow(this);
        windowSystem.AddWindow(mainWindow);
        highlighter = new NativeEquipmentHighlighter(this);
        CommandsAdd(CommandName);
        CommandsAdd("/sking");
        PluginInterface.UiBuilder.Draw += windowSystem.Draw;
        PluginInterface.UiBuilder.OpenMainUi += OpenMainUi;
        Framework.Update += OnFrameworkUpdate;
        Log.Information("SuitKing loaded (API {ApiLevel}).", PluginInterface.Manifest.DalamudApiLevel);
    }

    private void CommandsAdd(string command) => CommandManager.AddHandler(command,
        new CommandInfo(OnCommand) { HelpMessage = "打开或关闭套装之王套装装备列表。" });

    public void Dispose()
    {
        highlighter.Dispose();
        Framework.Update -= OnFrameworkUpdate;
        PluginInterface.UiBuilder.Draw -= windowSystem.Draw;
        PluginInterface.UiBuilder.OpenMainUi -= OpenMainUi;
        CommandManager.RemoveHandler(CommandName);
        CommandManager.RemoveHandler("/sking");
        windowSystem.RemoveAllWindows();
        Inventory.Clear();
    }

    private bool IsVisible(string name)
    {
        var addon = GameGui.GetAddonByName(name);
        return !addon.IsNull && addon.IsReady && addon.IsVisible;
    }

    private void OnFrameworkUpdate(IFramework framework)
    {
        var contentId = Player.ContentId;
        if (contentId != characterId)
        {
            characterId = contentId;
            Inventory.Clear();
            nextScan = 0;
            IsDresserOpen = false;
            selectionWasOpen = false;
            mainWindow.IsOpen = false;
        }
        var selectionOpen = contentId != 0 && IsVisible(SelectionAddonName);
        var open = contentId != 0 && (IsVisible(DresserAddonName) || selectionOpen);
        if (open != IsDresserOpen)
        {
            IsDresserOpen = open;
            if (open) mainWindow.OpenOnCompletable();
            else
            {
                mainWindow.IsOpen = false;
                highlighter.Restore();
            }
            Inventory.Clear();
            nextScan = 0;
        }
        if (selectionOpen && !selectionWasOpen)
        {
            mainWindow.OpenOnCompletable();
            nextScan = 0;
        }
        selectionWasOpen = selectionOpen;
        if (!open || Environment.TickCount64 < nextScan)
            return;
        nextScan = Environment.TickCount64 + 1000;
        try
        {
            Inventory.Refresh();
            lastError = null;
        }
        catch (Exception exception)
        {
            Inventory.Fail("读取失败，请查看 /xllog 中的 SuitKing 报错。");
            nextScan = Environment.TickCount64 + 5000;
            if (lastError != exception.Message)
                Log.Error(exception, "SuitKing could not read outfit equipment.");
            lastError = exception.Message;
        }
    }

    internal void RequestRefresh() => nextScan = 0;
    internal void SaveConfiguration() => PluginInterface.SavePluginConfig(Config);
    private void OnCommand(string command, string args)
    {
        if (mainWindow.IsOpen) mainWindow.IsOpen = false;
        else mainWindow.OpenOnCompletable();
    }
    private void OpenMainUi() => mainWindow.OpenOnCompletable();
}
