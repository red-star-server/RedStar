using Content.Shared._RedStar.Xenobiology.Slimes;
using Content.Shared.Nutrition.Prototypes;

namespace Content.Server._RedStar.Xenobiology.Slimes.Components;

/// <summary>
/// Prototype settings and current husbandry state for a slime. Runtime state is recalculated periodically.
/// </summary>
[RegisterComponent]
public sealed partial class SlimeHusbandryComponent : Component
{
    [DataField]
    public float OvercrowdingRadius = 2.5f;

    [DataField]
    public int RestlessThreshold = 3;

    [DataField]
    public int AggressiveThreshold = 5;

    [DataField]
    public SatiationValue RestlessBelow = "Okay";

    [DataField]
    public SatiationValue AggressiveBelow = "Starving";

    public int NearbySlimes;

    public SlimeTemperament Temperament;
}
