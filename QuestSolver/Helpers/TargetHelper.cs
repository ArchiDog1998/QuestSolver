using Dalamud.Game.ClientState.Objects.Types;
using ECommons.DalamudServices;
using ECommons.GameFunctions;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using Lumina.Excel.GeneratedSheets;

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
        Svc.Targets.Target = obj;
        return TargetSystem.Instance()->InteractWithObject(obj.Struct());
    }

    public static IEnumerable<IGameObject> GetInteractableTargets(Level level)
    {
        return Svc.Objects.Where(item => item.IsValid(level));
    }

    public static bool IsValid(this IGameObject obj, Level level) => level.IsInSide(obj) && obj.IsTargetable && obj.IsValid();

    public unsafe static uint GetNameplateIconId(this IGameObject obj) => obj.Struct()->NamePlateIconId;
}
