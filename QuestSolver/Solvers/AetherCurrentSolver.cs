using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Plugin.Services;
using ECommons.DalamudServices;
using ECommons.GameHelpers;
using FFXIVClientStructs.FFXIV.Client.Game;
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
    private readonly Dictionary<AetherCurrent, Level?> _points = [];

    public override uint Icon => 64;

    private unsafe static bool Unlocked(uint aetherId)
    {
        return PlayerState.Instance()->IsAetherCurrentUnlocked(aetherId);
    }

    private void FrameworkUpdate(IFramework framework)
    {
        var aether = Aethers.FirstOrDefault(a => !Unlocked(a.RowId));

        if (aether == null)
        {
            IsEnable = false;
            return;
        }

        var isStatic = aether.Quest.Row == 0;

        if (!_points.TryGetValue(aether, out var dest))
        {
            if (isStatic)
            {
                var eObj = Svc.Data.GetExcelSheet<EObj>()?.FirstOrDefault(e => e.Data == aether.RowId);
                var level = Svc.Data.GetExcelSheet<Level>()?.FirstOrDefault(e => e.Object == eObj?.RowId);
                _points[aether] = dest = level;
            }
            else
            {
                _points[aether] = dest = aether.Quest.Value?.IssuerLocation.Value;
            }
        }

        if (dest == null)
        {
            IsEnable = false;
            return;
        }

        if (isStatic)
        {
            if (!Available) return;

            if (MoveHelper.MoveTo(dest)) return;

            StaticAether(aether);
        }
        else if(aether.Quest.Value is not null
            && aether.Quest.Value.PreviousQuest.All(i => i.Row == 0 || QuestManager.IsQuestComplete(i.Row)))
        {
            QuestAether(aether, dest);
        }
        else
        {
            IsEnable = false;
        }
    }

    private static void QuestAether(AetherCurrent aether, Level dest)
    {
        if (Plugin.GetSolver<QuestFinishSolver>()?.IsEnable ?? false) return;

        if (!Available) return;

        if (MoveHelper.MoveTo(dest)) return;

        var tar = QuestGetterSolver.GetTarget(2);
        if (tar is not null)
        {
            TargetHelper.Interact(tar);
        }
    }

    private static void StaticAether(AetherCurrent aether)
    {
        if (MountHelper.InCombat) return;

        var obj = Svc.Objects.Where(o => o is not IPlayerCharacter
            && o.IsTargetable && !string.IsNullOrEmpty(o.Name.TextValue))
            .MinBy(i => Vector3.DistanceSquared(Player.Object.Position, i.Position));

        if (obj == null) return;
        Svc.Log.Info("Aether!");
        TargetHelper.Interact(obj);
    }

    protected override void Enable()
    {
        var set = Svc.Data.GetExcelSheet<AetherCurrentCompFlgSet>()?.FirstOrDefault(s => s.Territory.Row == Svc.ClientState.TerritoryType);

        if (set == null) return;

        Aethers = set.AetherCurrent
            .Where(i => i.Row != 0)
            .Select(i => i.Value)
            .OfType<AetherCurrent>()
            .ToArray();

        Plugin.IsEnableSolver<QuestGetterSolver>(false);

        Svc.AddonLifecycle.RegisterListener(AddonEvent.PostSetup, "JournalAccept", QuestGetterSolver.OnAddonJournalAccept);
        Svc.Framework.Update += FrameworkUpdate;
    }

    protected override void Disable()
    {
        Plugin.Vnavmesh.Stop();

        Svc.AddonLifecycle.UnregisterListener(QuestGetterSolver.OnAddonJournalAccept);
        Svc.Framework.Update -= FrameworkUpdate;
    }
}
