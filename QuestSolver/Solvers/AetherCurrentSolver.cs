using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Plugin.Services;
using ECommons.DalamudServices;
using ECommons.GameHelpers;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using Lumina.Excel.GeneratedSheets;
using QuestSolver.Helpers;
using System.ComponentModel;
using System.Numerics;

namespace QuestSolver.Solvers;

[Description("Aether Current")]
internal class AetherCurrentSolver : BaseSolver
{
    private AetherCurrent[] Aethers = [];
    private readonly Dictionary<AetherCurrent, Vector3?> _points = [];

    public override uint Icon => 64;

    private unsafe static bool Unlocked(uint aetherId)
    {
        return PlayerState.Instance()->IsAetherCurrentUnlocked(aetherId);
    }

    private void FrameworkUpdate(IFramework framework)
    {
        if (!Available) return;

        var aether = Aethers.FirstOrDefault(a => !Unlocked(a.RowId));

        if (aether == null)
        {
            Disable();
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
            Disable();
            return;
        }

        if (MoveHelper.MoveTo(dest.Value)) return;

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

    public override void Enable()
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

    public override void Disable()
    {
        Plugin.Vnavmesh.Stop();
        Svc.Framework.Update -= FrameworkUpdate;
    }
}
