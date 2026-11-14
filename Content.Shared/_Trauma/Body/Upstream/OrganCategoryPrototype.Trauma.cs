namespace Content.Shared.Body;

/// <summary>
/// Trauma - localization extensions
/// </summary>
public sealed partial class OrganCategoryPrototype
{
    public LocId NameLoc => $"markings-organ-{ID}";

    /// <summary>
    /// Get the localized name for this category.
    /// </summary>
    public string Name => Loc.GetString(NameLoc);

    /// <summary>
    /// Surgery stage depth required to operate on organs of this category
    /// (0 = closed, 1 = incised, 2 = bones opened). Used by OrganExposedCondition.
    /// </summary>
    [DataField]
    public int SurgeryDepth;
}
