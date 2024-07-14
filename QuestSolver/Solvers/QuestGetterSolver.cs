using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Plugin.Services;
using ECommons.DalamudServices;
using ECommons.GameFunctions;
using ECommons.GameHelpers;
using ECommons.MathHelpers;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Lumina.Excel.GeneratedSheets;
using Lumina.Excel.GeneratedSheets2;
using QuestSolver.Helpers;
using System.ComponentModel;
using System.Numerics;
using XIVConfigUI.Attributes;
namespace QuestSolver.Solvers;

[Description("Quest Getter")]
internal class QuestGetterSolver : BaseSolver
{
    public override uint Icon => 1;

    [Range(1, 50, ConfigUnitType.Yalms, 0.1f)]
    [UI("Search Range", Order = 1)]
    public float Range { get; set; } = 10;

    protected override void Enable()
    {
        Svc.Framework.Update += FrameworkUpdate;
    }

    protected override void Disable()
    {
        Svc.Framework.Update -= FrameworkUpdate;
    }

    private void FrameworkUpdate(IFramework framework)
    {
        if (Plugin.GetSolver<QuestFinishSolver>()?.IsEnable ?? false) return;

        ClickQuest();

        if (!Available) return;

        var tar = GetTarget(Range);
        if (tar != null)
        {
            if (!MoveHelper.MoveTo(tar.Position, 0))
            {
                TargetHelper.Interact(tar);
            }
        }
    }


    internal static unsafe IGameObject? GetTarget(float range)
    {
        var validTargets = Svc.Objects.Where(item => item.IsTargetable
            && Vector2.Distance(item.Position.ToVector2(), Player.Object.Position.ToVector2()) <= range * range);

        var tar = validTargets.FirstOrDefault(t => t.Struct()->NamePlateIconId is 71201); //MSQ
         
        tar ??= validTargets.FirstOrDefault(t => t.Struct()->NamePlateIconId is 71342); //Side Recycle
        tar ??= validTargets.FirstOrDefault(t => t.Struct()->NamePlateIconId is 71341); //Side

        tar ??= validTargets.FirstOrDefault(t => t.Struct()->NamePlateIconId is 71222); //Side Recycle
        tar ??= validTargets.FirstOrDefault(t => t.Struct()->NamePlateIconId is 71221); //Side

#if DEBUG
        if (tar == null)
        {
            foreach (var item in validTargets)
            {
                var icon = item.Struct()->NamePlateIconId;
                if (icon == 0) continue;

                Svc.Log.Error($"{item.Name} Name Place {icon}");
            }
        }
#endif

        return tar;
    }

    internal static unsafe void ClickQuest()
    {
        var result = (AtkUnitBase*)Svc.GameGui.GetAddonByName("JournalAccept");
        if (result == null || !result->IsVisible) return;

        if (CallbackHelper.Fire(result, true, 3, 4922)) //TODO: This is abnormal!
        {
            Plugin.EnableSolver<QuestFinishSolver>();
        }
    }
}
