using Content.Shared._RedStar.Genetics.Systems;
using Content.Shared.Damage.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RedStar.Genetics.Components;

/// <summary>
/// Damage reduction supplied by one gene manifestation. Reductions of active genes are added
/// before applying a single multiplier, independently of equipment and the host's native modifiers.
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(GeneResistanceSystem))]
public sealed partial class GeneResistanceComponent : Component
{
    /// <summary>
    /// Fractions of incoming positive damage prevented by type. Negative values add vulnerability.
    /// </summary>
    [DataField(required: true)]
    public Dictionary<ProtoId<DamageTypePrototype>, float> Reductions = new();

    /// <summary>
    /// Lower bound on the combined genetic damage multiplier. The strictest active bound wins.
    /// </summary>
    [DataField]
    public float MinimumMultiplier = 0.2f;
}
