namespace Content.Shared.Body;

public sealed partial class OrganCategoryPrototype
{
    public LocId NameLoc => $"markings-organ-{ID}";

    public string Name => Loc.GetString(NameLoc);

    /// <summary>
    /// Surgery stage depth required to operate on organs of this category
    /// (0 = closed, 1 = incised, 2 = bones opened). Used by OrganExposedCondition.
    /// </summary>
    [DataField]
    public int SurgeryDepth;
}
