using Content.Shared._RedStar.Genetics.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared._RedStar.Genetics.Components;

/// <summary>
/// Metabolism interval contribution supplied by a gene manifestation.
/// Deltas are added within genetics, then combined with other systems through their native APIs.
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(GeneMetabolismSystem))]
public sealed partial class GeneMetabolismComponent : Component
{
    /// <summary>
    /// Change to the metabolism interval multiplier. Positive values slow processing.
    /// </summary>
    [DataField]
    public float IntervalModifier;

    /// <summary>
    /// Lower bound on the combined genetic rate factor, preventing stalled or reversed processing.
    /// </summary>
    [DataField]
    public float MinimumMultiplier = 0.25f;

    /// <summary>
    /// Upper bound on the combined genetic rate factor. Independent modifiers remain separate.
    /// </summary>
    [DataField]
    public float MaximumMultiplier = 4f;
}
