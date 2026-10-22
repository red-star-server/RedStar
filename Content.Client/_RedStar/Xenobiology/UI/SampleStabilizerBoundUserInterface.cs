using Content.Shared._RedStar.Xenobiology.UI;
using Robust.Client.UserInterface;

namespace Content.Client._RedStar.Xenobiology.UI;

public sealed class SampleStabilizerBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    [ViewVariables]
    private SampleStabilizerWindow? _window;

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<SampleStabilizerWindow>();
        _window.Owner = Owner;
        _window.OnStabilization += (sampleA, sampleB) =>
            SendMessage(new SampleStabilizerUiStabilizationMessage(sampleA, sampleB));
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is not SampleStabilizerUiState stabilizerState)
            return;

        _window?.UpdateState(stabilizerState);
    }
}
