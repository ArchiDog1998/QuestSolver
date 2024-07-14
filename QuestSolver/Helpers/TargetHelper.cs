using Dalamud.Game.ClientState.Objects.Types;
using ECommons.DalamudServices;
using ECommons.GameFunctions;
using FFXIVClientStructs.FFXIV.Client.Game.Control;

namespace QuestSolver.Helpers;

internal static class TargetHelper
{
    public unsafe static ulong Interact(IGameObject obj)
    {
        Svc.Log.Info("Interact with " + obj.Name);
        return TargetSystem.Instance()->InteractWithObject(obj.Struct());
    }
}
