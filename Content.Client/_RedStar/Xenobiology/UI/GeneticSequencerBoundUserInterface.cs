using Content.Shared._RedStar.Xenobiology.UI;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client._RedStar.Xenobiology.UI;

[UsedImplicitly]
public sealed class GeneticSequencerBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    private GeneticSequencerWindow? _window;

    protected override void Open()
    {
        base.Open();
        _window = this.CreateWindow<GeneticSequencerWindow>();
        _window.OnDecode += (sample, family, order) =>
            SendMessage(new GeneticSequencerDecodeMessage(sample, family, order));
        _window.OnExtract += (sample, family, vial) =>
            SendMessage(new GeneticSequencerExtractMessage(sample, family, vial));
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);
        if (state is GeneticSequencerUiState sequencerState)
            _window?.UpdateState(sequencerState);
    }

    protected override void ReceiveMessage(BoundUserInterfaceMessage message)
    {
        base.ReceiveMessage(message);
        if (message is GeneticSequencerFeedbackMessage feedback)
            _window?.ShowFeedback(feedback);
    }
}
