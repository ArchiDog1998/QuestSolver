using Dalamud.Interface.Textures.TextureWraps;
using QuestSolver.Solvers;
using XIVConfigUI;

namespace QuestSolver.Windows;

internal class SolverItem(BaseSolver solver) : ConfigWindowItem
{
    public BaseSolver Solver => solver;

    internal SearchableCollection Collection { get; } = new SearchableCollection(solver);

    public override string Name => solver.GetType().Local();

    public override bool GetIcon(out IDalamudTextureWrap texture)
    {
        return ImageLoader.GetTexture(solver.Icon, out texture);
    }

    public override void Draw(ConfigWindow window)
    {
        Collection.DrawItems(0);
        base.Draw(window);
    }
}
