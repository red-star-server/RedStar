using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;

namespace Content.Shared._Trauma.Traumas;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(fieldDeltas: true)]
public sealed partial class BleedInflicterComponent : Component
{
    /// <summary>
    /// Whether this wound is currently producing blood loss.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool IsBleeding;

    /// <summary>
    ///     The severity it requires for the wound to have, so bleeds can be induced
    /// </summary>
    [DataField, AutoNetworkedField]
    public FixedPoint2 SeverityThreshold = FixedPoint2.Zero;

    /// <summary>
    /// Current bleeding amount produced by this wound.
    /// </summary>
    [DataField, AutoNetworkedField]
    public FixedPoint2 BleedingAmount = FixedPoint2.Zero;

    /// <summary>
    /// Accumulated penalty applied while updating bleeding severity.
    /// </summary>
    [DataField, AutoNetworkedField]
    public FixedPoint2 SeverityPenalty = FixedPoint2.New(1);

    /// <summary>
    /// Named bleeding overrides with priorities; the highest-priority override determines whether bleeding is allowed.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Dictionary<string, (int Priority, bool CanBleed)> BleedingModifiers = new();
}
