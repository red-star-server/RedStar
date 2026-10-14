using Content.Shared._RedStar.Xenobiology.UI;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client._RedStar.Xenobiology.UI;

[UsedImplicitly]
public sealed class CellSequencerBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    private CellSequencerWindow? _window;

    protected override void Open()
    {
        base.Open();
        _window = this.CreateWindow<CellSequencerWindow>();
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);
        if (state is CellSequencerUiState sequencerState)
            _window?.UpdateState(sequencerState);
    }
}
