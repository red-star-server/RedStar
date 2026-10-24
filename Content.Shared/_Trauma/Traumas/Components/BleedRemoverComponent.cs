using Robust.Shared.GameStates;
using Content.Shared.FixedPoint;

namespace Content.Shared._Trauma.Traumas;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class BleedRemoverComponent : Component
{
    /// <summary>
    ///     The severity it requires for the wound infliction to activate, so that space wont be activating this shit.
    /// </summary>
    [DataField, AutoNetworkedField]
    public FixedPoint2 SeverityThreshold = FixedPoint2.New(1);

    [ViewVariables(VVAccess.ReadOnly), AutoNetworkedField]
    public FixedPoint2 BleedingRemovalMultiplier = 0.30;
}
