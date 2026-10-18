using Content.Shared._RedStar.Genetics.UI;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client._RedStar.Genetics.UI;

[UsedImplicitly]
public sealed class GeneticAnalyzerBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    private GeneticAnalyzerWindow? _window;

    protected override void Open()
    {
        base.Open();
        _window = this.CreateWindow<GeneticAnalyzerWindow>();
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);
        if (state is GeneticAnalyzerUiState report)
            _window?.UpdateState(report);
    }
}
