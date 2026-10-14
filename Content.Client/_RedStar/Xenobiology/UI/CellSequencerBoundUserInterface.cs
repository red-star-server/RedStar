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
        _window.OnDecode += (sample, marker, order, revision, dish) =>
            SendMessage(new CellSequencerDecodeMessage(sample, marker, order, revision, dish));
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);
        if (state is CellSequencerUiState sequencerState)
            _window?.UpdateState(sequencerState);
    }
}
