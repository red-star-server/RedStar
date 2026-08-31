using Robust.Shared.GameStates;

namespace Content.Shared._RedStar.Sponsors;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SponsorGhostColorComponent : Component
{
    [DataField, AutoNetworkedField]
    public Color? Color;
}
