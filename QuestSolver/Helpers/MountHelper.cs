using Dalamud.Game.ClientState.Conditions;
using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Client.Game;

namespace QuestSolver.Helpers;
internal static class MountHelper
{
    public static bool IsFlying => Svc.Condition[ConditionFlag.InFlight] || Svc.Condition[ConditionFlag.Diving];
    public static bool IsMount => Svc.Condition[ConditionFlag.Mounted];
    public static bool IsJumping => Svc.Condition[ConditionFlag.Jumping];
    public static bool InCombat => Svc.Condition[ConditionFlag.InCombat];

    public static void TryFly()
    {
        if (!IsMount || IsFlying) return;
        TryJump();
    }
    public static bool TryMount() => !IsMount && TryGeneralAction(9);
    public static bool TryJump() => TryGeneralAction(2);
    public static void TryDisMount() => TryGeneralAction(23);

    private static bool TryGeneralAction(uint generalAction)
    {
        if (!CanUseGeneralAction(generalAction)) return false;
        return UseGeneralAction(generalAction);
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
