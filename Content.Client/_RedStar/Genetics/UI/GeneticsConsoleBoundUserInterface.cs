using Content.Shared._RedStar.Genetics.UI;
using Content.Shared._RedStar.Genetics.Events;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client._RedStar.Genetics.UI;

[UsedImplicitly]
public sealed class GeneticsConsoleBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    private GeneticsConsoleWindow? _window;

    protected override void Open()
    {
        base.Open();
        _window = this.CreateWindow<GeneticsConsoleWindow>();
        _window.ProcedureRequested += (patient, gene, procedure) => SendMessage(new GeneticsProcedureMessage(patient, gene, procedure));
        _window.CancelRequested += () => SendMessage(new GeneticsCancelMessage());
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);
        if (state is GeneticsConsoleUiState report)
            _window?.UpdateState(report);
    }
}
