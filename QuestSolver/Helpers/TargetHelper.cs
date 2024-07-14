﻿using Dalamud.Game.ClientState.Objects.Types;
using ECommons.DalamudServices;
using ECommons.GameFunctions;
using FFXIVClientStructs.FFXIV.Client.Game.Control;

namespace QuestSolver.Helpers;

internal static class TargetHelper
{
    private static DateTime _lastCall = DateTime.Now;

    public unsafe static ulong Interact(IGameObject obj)
    {
        if (MountHelper.InCombat) return 1;
        if (DateTime.Now - _lastCall < TimeSpan.FromSeconds(1)) return 1;
        _lastCall = DateTime.Now;

        Svc.Log.Info("Interact with " + obj.Name);
        return TargetSystem.Instance()->InteractWithObject(obj.Struct());
    }
}
