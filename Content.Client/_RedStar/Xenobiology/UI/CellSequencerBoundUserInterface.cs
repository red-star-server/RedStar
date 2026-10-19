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
        _window.OnDecode += (sample, family, order) =>
            SendMessage(new CellSequencerDecodeMessage(sample, family, order));
        _window.OnExtract += (sample, family, vial) =>
            SendMessage(new CellSequencerExtractMessage(sample, family, vial));
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);
        if (state is CellSequencerUiState sequencerState)
            _window?.UpdateState(sequencerState);
    }

    protected override void ReceiveMessage(BoundUserInterfaceMessage message)
    {
        base.ReceiveMessage(message);
        if (message is CellSequencerFeedbackMessage feedback)
            _window?.ShowFeedback(feedback);
    }
}
