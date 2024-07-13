using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Plugin.Services;
using ECommons.DalamudServices;
using ECommons.GameHelpers;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using Lumina.Excel.GeneratedSheets;
using System.Numerics;

namespace QuestSolver.Solvers;
internal class AetherCurrentSolver : IDisposable
{
    private AetherCurrent[] Aethers = [];
    private Dictionary<AetherCurrent, Vector3?> _points = [];


    public AetherCurrentSolver()
    {
        var set = Svc.Data.GetExcelSheet<AetherCurrentCompFlgSet>()?.FirstOrDefault(s => s.Territory.Row == Svc.ClientState.TerritoryType);

        if (set == null) return;

        Aethers = set.AetherCurrent
            .Where(i => i.Row != 0)
            .Select(i => i.Value)
            .OfType<AetherCurrent>()
            .Where(i => i.Quest.Row == 0)
            .ToArray();

        Svc.Framework.Update += FrameworkUpdate;
    }
    private unsafe static bool Unlocked(uint aetherId)
    {
        return PlayerState.Instance()->IsAetherCurrentUnlocked(aetherId);
    }

    Vector3 lastPos = default;
    DateTime stopTime = DateTime.Now;
    private void FrameworkUpdate(IFramework framework)
    {
        if (!Player.Available) return;
        if (Svc.Condition[ConditionFlag.Occupied]) return;
        if (Svc.Condition[ConditionFlag.OccupiedInQuestEvent]) return;
        if (Svc.Condition[ConditionFlag.OccupiedInEvent]) return;
        if (Svc.Condition[ConditionFlag.Occupied38]) return;
        if (Svc.Condition[ConditionFlag.Unknown57]) return;
        if (Svc.Condition[ConditionFlag.Casting]) return;
        if (Svc.Condition[ConditionFlag.OccupiedInCutSceneEvent]) return;

        if (Plugin.Vnavmesh.IsRunning())
        {
            var dis = Vector3.DistanceSquared(Player.Object.Position, lastPos);
            if (dis < 0.001 && DateTime.Now - stopTime > TimeSpan.FromSeconds(3))
            {
                Plugin.Vnavmesh.Stop();
                stopTime = DateTime.Now;
            }
            lastPos = Player.Object.Position;
            return;
        }

        var aether = Aethers.FirstOrDefault(a => !Unlocked(a.RowId));

        if (aether == null)
        {
            Dispose();
            return;
        }

        if (!_points.TryGetValue(aether, out var dest))
        {
            var eObj = Svc.Data.GetExcelSheet<EObj>()?.FirstOrDefault(e => e.Data == aether.RowId);
            var level = Svc.Data.GetExcelSheet<Level>()?.FirstOrDefault(e => e.Object == eObj?.RowId);
            if (level == null)
            {
                _points[aether] = dest = null;
            }
            else
            {
                _points[aether] = dest = new Vector3(level.X, level.Y, level.Z);
            }
        }

        if (dest == null)
        {
            Dispose();
            return;
        }

        
        if (Vector3.DistanceSquared(Player.Object.Position, dest.Value) > 1)
        {
            if (!Svc.Condition[ConditionFlag.Mounted])
            {
                Svc.Log.Info("Mounting!");
                unsafe
                {
                    ActionManager.Instance()->UseAction(ActionType.GeneralAction, 9);
                }
            }
            else
            {
                if (!Plugin.Vnavmesh.PathfindInProgress())
                {
                    Plugin.Vnavmesh.PathfindAndMoveTo(dest.Value, false);
                }
            }
        }
        else
        {
            var obj = Svc.Objects.Where(o => o is not IPlayerCharacter
                && o.IsTargetable && !string.IsNullOrEmpty(o.Name.TextValue))
                .MinBy(i => Vector3.DistanceSquared(Player.Object.Position, i.Position));

            if (obj == null) return;

            unsafe
            {
                Svc.Log.Info("Aether!");

                var tar = (FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject*)obj?.Address;
                TargetSystem.Instance()->InteractWithObject(tar);
            }
        }
    }

    public void Dispose()
    {
        Plugin.Vnavmesh.Stop();
        Svc.Framework.Update -= FrameworkUpdate;
    }
}
