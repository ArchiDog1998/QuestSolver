using Dalamud.Game.ClientState.Conditions;
using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Client.Game;

namespace QuestSolver.Helpers;
internal static class MountHelper
{
    public static bool IsFlying => Svc.Condition[ConditionFlag.InFlight] || Svc.Condition[ConditionFlag.Diving];
    public static bool IsMount => Svc.Condition[ConditionFlag.Mounted];
    public static bool IsJumping => Svc.Condition[ConditionFlag.Jumping];

    public static void TryFly()
    {
        if (!IsMount) return;
        TryJump();
    }
    public static void TryMount() => TryGeneralAction(9);
    public static void TryJump() => TryGeneralAction(2);
    public static void TryDisMount() => TryGeneralAction(23);

    private static void TryGeneralAction(uint generalAction)
    {
        if (!CanUseGeneralAction(generalAction)) return;
        UseGeneralAction(generalAction);
    }

    public static unsafe bool CanUseGeneralAction(uint generalAction)
    {
        return ActionManager.Instance()->GetActionStatus(ActionType.GeneralAction, generalAction) == 0;
    }

    public static unsafe bool UseGeneralAction(uint generalAction)
    {
        return ActionManager.Instance()->UseAction(ActionType.GeneralAction, generalAction);
    }
}
