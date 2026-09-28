using Content.Shared._RedStar.Xenobiology;
using Robust.Client.UserInterface;

namespace Content.Client._RedStar.Xenobiology.UI;

public sealed class XenobiologyConsoleBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    private XenobiologyConsoleWindow? _window;

    protected override void Open()
    {
        base.Open();
        _window = this.CreateWindow<XenobiologyConsoleWindow>();
        _window.ScannerSelected += scanner => SendMessage(new XenobiologyConsoleSelectScannerMessage(scanner));
        _window.SlimeSelected += slime => SendMessage(new XenobiologyConsoleSelectSlimeMessage(slime));
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        if (state is XenobiologyConsoleUiState console)
            _window?.UpdateState(console);
    }
}
