using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Shared._RedStar.Xenobiology.Slimes.Components;

/// <summary>
/// This component describes the current state of the slime.
/// </summary>
[RegisterComponent]
public sealed partial class SlimeComponent : Component
{
    /// <summary>
    /// The amount of damage dealt to the target entity when the slime eats.
    /// </summary>
    [DataField(required: true)]
    public DamageSpecifier DamageOnEat;

    /// <summary>
    /// The amount of nutrition the slime gains on each eat.
    /// </summary>
    [DataField(required: true)]
    public FixedPoint2 NutritionOnHit;

    /// <summary>
    /// The extract this slime provides when processed in the Slime Processor.
    /// </summary>
    [DataField(required: true)]
    public EntProtoId Extract;
}
