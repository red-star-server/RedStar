using Content.Shared.Paper;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared._RedStar.Photocopier;

/// <summary>
/// Describes a paperwork form that can be selected and printed from a photocopier.
/// </summary>
[Prototype]
public sealed partial class PaperworkFormPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// The category under which this form is displayed.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<PaperworkCategoryPrototype> Category { get; private set; }

    /// <summary>
    /// The localized title displayed for this form.
    /// </summary>
    [DataField(required: true)]
    public LocId Name { get; private set; }

    /// <summary>
    /// The resource path to the XML template used to populate the printed form.
    /// </summary>
    [DataField(required: true)]
    public ResPath Template { get; private set; }

    /// <summary>
    /// The paper entity prototype spawned when this form is printed.
    /// </summary>
    [DataField(required: true)]
    public EntProtoId<PaperComponent> PaperPrototype { get; private set; }
}
