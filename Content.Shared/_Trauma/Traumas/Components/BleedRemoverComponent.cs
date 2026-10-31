using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;

namespace Content.Shared._Trauma.Traumas;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class BleedRemoverComponent : Component
{
    /// <summary>
    /// Minimum wound severity required to trigger this effect.
    /// </summary>
    [DataField, AutoNetworkedField]
    public FixedPoint2 SeverityThreshold = FixedPoint2.New(1);

    [ViewVariables(VVAccess.ReadOnly), AutoNetworkedField]
    public FixedPoint2 BleedingRemovalMultiplier = 0.30;
}
