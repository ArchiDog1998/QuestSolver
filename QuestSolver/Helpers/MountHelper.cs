using Dalamud.Game.ClientState.Conditions;
using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Client.Game;

namespace QuestSolver.Helpers;
internal static class MountHelper
{
    private readonly static HashSet<uint> _cantFly = [];
    private static DateTime? _jumpedTime;
    public static bool IsFlying => Svc.Condition[ConditionFlag.InFlight] || Svc.Condition[ConditionFlag.Diving];
    public static bool CanFly => !_cantFly.Contains(Svc.ClientState.TerritoryType);
    public static bool IsMount => Svc.Condition[ConditionFlag.Mounted];
    public static bool CanMount => CanUseGeneralAction(ActionType.GeneralAction, 9);

    public static bool IsJumping => Svc.Condition[ConditionFlag.Jumping];
    public static bool InCombat => Svc.Condition[ConditionFlag.InCombat];
    public static void TryFly()
    {
        if (!IsMount || IsFlying) return;

        if (DateTime.Now - _jumpedTime > TimeSpan.FromSeconds(0.5f)) //Can't Fly
        {
            Svc.Log.Info("Can't Fly at " + Svc.ClientState.TerritoryType);
            _cantFly.Add(Svc.ClientState.TerritoryType);
        }
        else if(TryJump()) // Try to fly..
        {
            Svc.Log.Info("Jumped at " + Svc.ClientState.TerritoryType);
            _jumpedTime = DateTime.Now;
        }
    }

    public static void Init()
    {
        Svc.ClientState.TerritoryChanged += ClientState_TerritoryChanged;
    }

    private static void ClientState_TerritoryChanged(ushort obj)
    {
        _jumpedTime = null;
    }

    public static void Dispose()
    {
        Svc.ClientState.TerritoryChanged -= ClientState_TerritoryChanged;
    }

    public static bool TryMount()
    {
        if (IsMount) return false;
        if (Plugin.Settings.MountId == 0)
        {
            return TryGeneralAction(9);
        }
        else
        {
            return UseAction(ActionType.Mount, Plugin.Settings.MountId);
        }
    }

    public static bool TryJump()
    {
        if(IsJumping) return false;
        return TryGeneralAction(2);
    }
    public static void TryDisMount() => UseAction(ActionType.GeneralAction, 23);

    private static bool TryGeneralAction(uint generalAction)
    {
        if (!CanUseGeneralAction(ActionType.GeneralAction, generalAction)) return false;
        return UseAction(ActionType.GeneralAction, generalAction);
    }

    public static unsafe bool CanUseGeneralAction(ActionType actionType, uint generalAction)
    {
        return ActionManager.Instance()->GetActionStatus(actionType, generalAction) == 0;
    }

    public static unsafe bool UseAction(ActionType actionType, uint generalAction)
    {
        return ActionManager.Instance()->UseAction(actionType, generalAction);
    }
}
