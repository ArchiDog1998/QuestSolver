using Dalamud.Game.Gui.Dtr;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using ECommons;
using ECommons.Automation;
using ECommons.DalamudServices;
using ECommons.GameHelpers;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Lumina.Excel.GeneratedSheets;
using QuestSolver.Configuration;
using QuestSolver.Helpers;
using QuestSolver.IPC;
using QuestSolver.Solvers;
using QuestSolver.Windows;
using XIVConfigUI;

namespace QuestSolver;
internal class Plugin : IDalamudPlugin
{
    private static WindowSystem _windowSystem = null!;
    private static SettingsWindow _settingsWindow = null!;
    public static Settings Settings { get; private set; } = null!;
    public static VnavmeshManager Vnavmesh { get; private set; } = null!;

    public Plugin(IDalamudPluginInterface pluginInterface)
    {
        ECommonsMain.Init(pluginInterface, this);
        XIVConfigUIMain.Init(pluginInterface, "/quest", "Opens the Quest Solver configuration window.\n /quest Cancel to cancel all things.", PluginCommand);

#if DEBUG
        Callback.InstallHook();
#endif

        Svc.PluginInterface.UiBuilder.Draw += Draw;
        Svc.PluginInterface.UiBuilder.OpenConfigUi += OpenConfigUi;
        Svc.PluginInterface.UiBuilder.OpenMainUi += OpenConfigUi;
        Svc.Framework.Update += Framework_Update;

        MountHelper.Init();

        try
        {
            Settings = pluginInterface.GetPluginConfig() as Settings ?? new Settings();
        }
        catch
        {
            Settings = new Settings();
        }

        Vnavmesh = new VnavmeshManager();
        CreateWindows();

        unsafe
        {
            //EventHelper.SendEvent(AgentId.LeveQuest, 0, 3, 773);


            Leve l;
            //var talk = (AtkUnitBase*)Svc.GameGui.GetAddonByName("GuildLeve");
            var talk = (AtkUnitBase*)Svc.GameGui.GetAddonByName("SelectString");

            Callback.Fire(talk, true, -1);
        }
    }

    private IDtrBarEntry? bar;
    private void Framework_Update(IFramework framework)
    {
        bar ??= Svc.DtrBar.Get("Quest Solver");

        if (!bar.Shown) bar.Shown = true;

        var solver = GetSolver<QuestFinishSolver>();
        var payloads = solver?.QuestItem?.Quest.Name.RawString;
        bar.Text = new SeString(
            new IconPayload(BitmapFontIcon.FanFestival),
             new TextPayload(payloads ?? "Quest")
            );

        bar.OnClick ??= new(Cancel);
    }

    public static void IsEnableSolver<T>(bool isEnable = true) where T : BaseSolver
    {
        var solver = GetSolver<T>();
        if (solver == null) return;
        solver.IsEnable = isEnable;
    }

    public static T? GetSolver<T>() where T : BaseSolver
    {
        return _settingsWindow.Items.OfType<SolversItem>().SelectMany(i => i.Solvers).OfType<T>().FirstOrDefault();
    }

    public static BaseSolver[] GetSolvers(params Type[] types)
    {
        return _settingsWindow.Items.OfType<SolversItem>().SelectMany(i => i.Solvers).Where(i => types.Contains(i.GetType())).ToArray();
    }

    public void Dispose()
    {
        Settings.Save();

        Cancel();

        _windowSystem.RemoveAllWindows();

        MountHelper.Dispose();

#if DEBUG
        Callback.UninstallHook();
#endif
        XIVConfigUIMain.Dispose();
        ECommonsMain.Dispose();

        Svc.Framework.Update -= Framework_Update;
        Svc.PluginInterface.UiBuilder.Draw -= Draw;
        Svc.PluginInterface.UiBuilder.OpenConfigUi -= OpenConfigUi;
        Svc.PluginInterface.UiBuilder.OpenMainUi -= OpenConfigUi;

        bar?.Remove();
        GC.SuppressFinalize(this);
    }

    private void Draw()
    {
        if (Settings == null || !Player.Available) return;
        if (Svc.GameGui.GameUiHidden) return;

        _windowSystem?.Draw();
    }

    private static void CreateWindows()
    {
        _settingsWindow = new SettingsWindow();

        _windowSystem = new WindowSystem("QuestSolver_Windows");
        _windowSystem.AddWindow(_settingsWindow);
    }

    public static void OpenConfigUi()
    {
        _settingsWindow.IsOpen = true;
    }

    public static void PluginCommand(string str)
    {
        if (str == "Cancel")
        {
            Cancel();
        }

        _settingsWindow.Toggle();
    }

    private static void Cancel()
    {
        foreach (var entry in _settingsWindow.Items)
        {
            if (entry is not SolversItem solvers) continue;

            foreach (var item in solvers.Solvers)
            {
                item.IsEnable = false;
            }
        }
    }
}
