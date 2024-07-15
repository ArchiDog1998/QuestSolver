using ECommons.Automation;
using System.ComponentModel;

namespace QuestSolver.Solvers;

[Description("Skip Cutscene")]
internal class SkipCutscene : BaseSolver
{
    public override uint Icon => 1;

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
