using Content.Shared.Research.Components;
using Content.Shared._RedStar.Xenobiology.UI;
using Robust.Client.UserInterface;

namespace Content.Client._RedStar.Xenobiology.UI;

public sealed class CellularFusionBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    [ViewVariables]
    private CellularFusionWindow? _window;

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<CellularFusionWindow>();
        _window.SetEntity(Owner);
        _window.OnServerSelection += () => SendMessage(new ConsoleServerSelectionMessage());
        _window.OnSplice += (cellA, cellB) => SendMessage(new CellularFusionUiSpliceMessage(cellA, cellB));
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is not CellularFusionUiState cellularFusionUiState)
            return;

        _window?.UpdateState(cellularFusionUiState);
    }
}
