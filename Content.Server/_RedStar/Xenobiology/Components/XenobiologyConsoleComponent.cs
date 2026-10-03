using Content.Shared._RedStar.Xenobiology.UI;
namespace Content.Server._RedStar.Xenobiology.Components;

[RegisterComponent]
public sealed partial class XenobiologyConsoleComponent : Component
{
    public TimeSpan NextUpdate;
    public EntityUid? SelectedScanner;
    public EntityUid? SelectedSlime;
    public XenobiologyConsoleUiState? LastState;
}
