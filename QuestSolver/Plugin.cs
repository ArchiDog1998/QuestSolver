using Dalamud.Interface.Windowing;
using Dalamud.Plugin;
using ECommons;
using ECommons.Automation;
using ECommons.DalamudServices;
using ECommons.GameHelpers;
using QuestSolver.Configuration;
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
        XIVConfigUIMain.Init(pluginInterface, "/quest", "Opens the Quest Solver configuration window.", PluginCommand);

#if DEBUG
        Callback.InstallHook();
#endif

        Svc.PluginInterface.UiBuilder.Draw += Draw;
        Svc.PluginInterface.UiBuilder.OpenConfigUi += OpenConfigUi;
        Svc.PluginInterface.UiBuilder.OpenMainUi += OpenConfigUi;

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
    }
    public static void EnableSolver<T>() where T : BaseSolver
    {
        var solver = GetSolver<T>();
        if (solver == null) return;
        solver.IsEnable = true;
    }

    public static T? GetSolver<T>() where T : BaseSolver
    {
        return _settingsWindow.Items.OfType<SolverItem>().Select(i => i.Solver).OfType<T>().FirstOrDefault();
    }

    public void Dispose()
    {
        Settings.Save();

        foreach (var item in _settingsWindow.Items.OfType<SolverItem>())
        {
            item.Solver.IsEnable = false;
        }

        _windowSystem.RemoveAllWindows();

#if DEBUG
        Callback.UninstallHook();
#endif
        XIVConfigUIMain.Dispose();
        ECommonsMain.Dispose();

        Svc.PluginInterface.UiBuilder.Draw -= Draw;
        Svc.PluginInterface.UiBuilder.OpenConfigUi -= OpenConfigUi;
        Svc.PluginInterface.UiBuilder.OpenMainUi -= OpenConfigUi;
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
        _settingsWindow.Toggle();
    }
}
