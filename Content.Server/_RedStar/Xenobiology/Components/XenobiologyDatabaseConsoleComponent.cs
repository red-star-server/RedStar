using Content.Shared._RedStar.Xenobiology.UI;

namespace Content.Server._RedStar.Xenobiology.Components;

[RegisterComponent]
public sealed partial class XenobiologyDatabaseConsoleComponent : Component
{
    public XenobiologyDatabaseConsoleUiState? LastState;
}
