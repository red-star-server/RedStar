using Content.Shared._RedStar.Xenobiology.UI;
using Robust.Client.UserInterface;

namespace Content.Client._RedStar.Xenobiology.UI;

public sealed class XenobiologyDatabaseConsoleBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    private XenobiologyDatabaseConsoleWindow? _window;

    protected override void Open()
    {
        base.Open();
        _window = this.CreateWindow<XenobiologyDatabaseConsoleWindow>();
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        if (state is XenobiologyDatabaseConsoleUiState database)
            _window?.UpdateState(database);
    }
}
