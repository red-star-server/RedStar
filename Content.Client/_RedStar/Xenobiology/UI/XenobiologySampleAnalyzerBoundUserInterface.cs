using Content.Shared._RedStar.Xenobiology;
using Content.Shared.Research.Components;
using Robust.Client.UserInterface;

namespace Content.Client._RedStar.Xenobiology.UI;

public sealed class XenobiologySampleAnalyzerBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    private XenobiologySampleAnalyzerWindow? _window;

    protected override void Open()
    {
        base.Open();
        _window = this.CreateWindow<XenobiologySampleAnalyzerWindow>();
        _window.AnalyzeRequested += () => SendMessage(new XenobiologyAnalyzeSampleMessage());
        _window.ServerRequested += () => SendMessage(new ConsoleServerSelectionMessage());
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        if (state is XenobiologySampleAnalyzerUiState analyzer)
            _window?.UpdateState(analyzer);
    }
}
