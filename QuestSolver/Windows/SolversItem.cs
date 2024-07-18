using Dalamud.Interface.Textures.TextureWraps;
using Dalamud.Interface.Utility.Raii;
using ImGuiNET;
using QuestSolver.Solvers;
using XIVConfigUI;

namespace QuestSolver.Windows;

internal class SolversItem(SolverItemType type, params BaseSolver[] solvers) : ConfigWindowItem
{
    public BaseSolver[] Solvers => solvers;

    internal SearchableCollection[] Collections { get; } = [..solvers.Select(s => new SearchableCollection(s))];

    public override string Name => type.Local();

    public override bool GetIcon(out IDalamudTextureWrap texture)
    {
        return ImageLoader.GetTexture((uint)type, out texture);
    }

    public override void Draw(ConfigWindow window)
    {
        for (int i = 0; i < Collections.Length; i++)
        {
            var solver = Solvers[i];
            if (ImGui.CollapsingHeader(solver.GetType().Local()))
            {
                var desc = solver.Description;

                if (!string.IsNullOrEmpty(desc))
                {
                    ImGui.TextWrapped(solver.GetType().Local("Description", desc));
                }

                using var id = ImRaii.PushId(i);
                Collections[i].DrawItems(0);
            }
        }
        base.Draw(window);
    }
}
