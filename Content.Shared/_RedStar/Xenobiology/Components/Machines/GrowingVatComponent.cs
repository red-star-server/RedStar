using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;

namespace Content.Shared._RedStar.Xenobiology.Components.Machines;

/// <summary>
/// Configures one genetic cultivation cycle and tracks its current subject.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class GrowingVatComponent : Component
{
    [DataField]
    public string Solution = "vat";

    [DataField]
    public FixedPoint2 MinimumNutriment = FixedPoint2.New(5);

    [DataField]
    public FixedPoint2 MinimumSaline = FixedPoint2.New(5);

    [DataField]
    public FixedPoint2 NutrimentPerSecond = FixedPoint2.New(0.1f);

    [DataField]
    public FixedPoint2 SalinePerSecond = FixedPoint2.New(0.1f);

    [DataField]
    public TimeSpan CycleDuration = TimeSpan.FromSeconds(60);

    [DataField]
    public float MutationChance = 1f;

    [ViewVariables]
    public EntityUid? Subject;

    [ViewVariables]
    public TimeSpan Progress;

    [ViewVariables]
    public float TickAccumulator;
}
