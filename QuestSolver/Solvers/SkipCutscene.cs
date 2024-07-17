using ECommons.Automation;
using QuestSolver.Windows;
using System.ComponentModel;

namespace QuestSolver.Solvers;

[Description("Skip Cutscene")]
internal class SkipCutscene : BaseSolver
{
    public override SolverItemType ItemType => SolverItemType.UI;

    private bool _init =false;
    protected override void Disable()
    {
        AutoCutsceneSkipper.Disable();
    }

    protected override void Enable()
    {
        if (!_init)
        {
            AutoCutsceneSkipper.Init(i => true);
            _init = true;
        }
        
        AutoCutsceneSkipper.Enable();
    }
}
