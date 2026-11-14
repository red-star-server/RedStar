using Robust.Shared.Prototypes;

namespace Content.Shared._Trauma.Surgery.Graph;

/// <summary>
/// An anatomical state of a body part (Closed, Incised, SkullOpen, ...).
/// Procedures transition parts between stages.
/// </summary>
[Prototype]
public sealed partial class SurgeryStagePrototype : IPrototype
{
    [ViewVariables]
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField]
    public LocId Name;

    /// <summary>
    /// How deep the part is opened: Closed = 0, Incised = 1, SkullOpen/RibcageOpen = 2.
    /// Compared against OrganCategoryPrototype.SurgeryDepth by OrganExposedCondition.
    /// </summary>
    [DataField]
    public int Depth;

    [DataField]
    public List<SurgeryOverlay> Overlays = [];
}

[DataDefinition]
public sealed partial class SurgeryOverlay
{
    [DataField(required: true)]
    public string Rsi = default!;

    [DataField(required: true)]
    public string StateSuffix = default!;
}