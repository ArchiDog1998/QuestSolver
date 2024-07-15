using Dalamud.Interface.Windowing;
using Dalamud.Plugin;
using ECommons;
using ECommons.Automation;
using ECommons.DalamudServices;
using ECommons.GameHelpers;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using QuestSolver.Configuration;
using QuestSolver.Helpers;
using QuestSolver.IPC;
using QuestSolver.Solvers;
using QuestSolver.Windows;
using XIVConfigUI;
using ECommons.Automation.UIInput;
using ECommons.Schedulers;
using System;

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
    }

    public static void IsEnableSolver<T>(bool isEnable = true) where T : BaseSolver
    {
        var solver = GetSolver<T>();
        if (solver == null) return;
        solver.IsEnable = isEnable;
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

        MountHelper.Dispose();

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
        if (str == "Cancel")
        {
            foreach (var entry in _settingsWindow.Items)
            {
                if (entry is not SolverItem solver) continue;
                solver.Solver.IsEnable = false;
            }
        }

        _settingsWindow.Toggle();
    }
}
