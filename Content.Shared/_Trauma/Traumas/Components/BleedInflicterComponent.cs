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

    [ViewVariables]
    public FixedPoint2 BleedingAmount => BleedingAmountRaw * Scaling;

    /// <summary>
    /// Bleeding contribution before the current scaling multiplier is applied.
    /// </summary>
    [DataField, AutoNetworkedField]
    public FixedPoint2 BleedingAmountRaw = FixedPoint2.Zero;

    // these are calculated when wound is spawned.
    /// <summary>
    /// Time at which the bleeding scaling transition finishes.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly), AutoNetworkedField]
    public TimeSpan ScalingFinishesAt = TimeSpan.Zero;

    /// <summary>
    /// Time at which the bleeding scaling transition started.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly), AutoNetworkedField]
    public TimeSpan ScalingStartsAt = TimeSpan.Zero;

    /// <summary>
    /// Multiplier used to determine how quickly bleeding changes with wound severity.
    /// </summary>
    [DataField]
    public FixedPoint2 ScalingSpeed = FixedPoint2.New(1);

    /// <summary>
    /// Accumulated penalty applied while updating bleeding severity.
    /// </summary>
    [DataField, AutoNetworkedField]
    public FixedPoint2 SeverityPenalty = FixedPoint2.New(1);

    /// <summary>
    /// Current multiplier applied to the wound's raw bleeding amount.
    /// </summary>
    [DataField, AutoNetworkedField]
    public FixedPoint2 Scaling = FixedPoint2.New(1);

    /// <summary>
    /// Upper limit for the wound's bleeding scaling multiplier.
    /// </summary>
    [DataField, AutoNetworkedField]
    public FixedPoint2 ScalingLimit = FixedPoint2.New(1.4);

    /// <summary>
    /// Named bleeding overrides with priorities; the highest-priority override determines whether bleeding is allowed.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Dictionary<string, (int Priority, bool CanBleed)> BleedingModifiers = new();
}
