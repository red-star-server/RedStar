using Content.Shared._RedStar.Xenobiology.UI;
using Robust.Client.UserInterface;

namespace Content.Client._RedStar.Xenobiology.UI.Scanner;

public sealed class XenobiologyScannerBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    private XenobiologyScannerWindow? _window;
    private XenobiologyScannerScannedMessage? _pending;

    protected override void Open()
    {
        base.Open();
        _window = this.CreateWindow<XenobiologyScannerWindow>();
        if (_pending is not { } message)
            return;

        _window.Populate(message);
        _pending = null;
    }

    protected override void ReceiveMessage(BoundUserInterfaceMessage message)
    {
        if (message is not XenobiologyScannerScannedMessage scanned)
            return;

        if (_window is { } window)
            window.Populate(scanned);
        else
            _pending = scanned;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        _window?.Close();
        _window = null;
        _pending = null;
    }
}
