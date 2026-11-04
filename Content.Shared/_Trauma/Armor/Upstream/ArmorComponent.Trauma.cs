using Content.Shared._Trauma.Body;
using Content.Shared._Trauma.Common.Body;
using Content.Shared._Trauma.Common.Traumas;
using Content.Shared.FixedPoint;

namespace Content.Shared.Armor;

/// <summary>
/// Trauma - limb damage, trauma and AP related fields.
/// </summary>
public sealed partial class ArmorComponent
{
    /// <summary>
    /// If true, armor coverage is hidden when examined.
    /// </summary>
    [DataField("coverageHidden")]
    public bool ArmourCoverageHidden;

    /// <summary>
    /// If true, armor modifiers are hidden when examined.
    /// </summary>
    [DataField("modifiersHidden")]
    public bool ArmourModifiersHidden;

    /// <summary>
    /// Body-part types covered by this armor, independent of symmetry.
    /// </summary>
    [DataField("coverage")]
    [Access(typeof(SharedArmorSystem), typeof(CoveredPartSystem))]
    public List<BodyPartType> ArmorCoverage = new();

    /// <summary>
    /// Trauma severity deductions provided by this armor.
    /// </summary>
    [DataField]
    public Dictionary<TraumaType, FixedPoint2> TraumaDeductions = new()
    {
        { TraumaType.Dismemberment, 0 },
        { TraumaType.BoneDamage, 0 },
        { TraumaType.OrganDamage, 0 },
        { TraumaType.VeinsDamage, 0 },
        { TraumaType.NerveDamage, 0 }
    };
}
