using Content.Shared._RedStar.Research;
using Content.Shared.Research.Components;
using Robust.Client.UserInterface;

namespace Content.Client._RedStar.Research.UI;

public sealed class PointDiskConsoleBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    private PointDiskConsoleMenu? _menu;

    protected override void Open()
    {
        base.Open();
        _menu = this.CreateWindow<PointDiskConsoleMenu>();
        _menu.OnServerSelected += () => SendMessage(new ConsoleServerSelectionMessage());
        _menu.OnPrint += disk => SendMessage(new PointDiskConsolePrintMessage(disk));
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);
        if (state is PointDiskConsoleState consoleState)
            _menu?.UpdateState(consoleState);
    }
}
