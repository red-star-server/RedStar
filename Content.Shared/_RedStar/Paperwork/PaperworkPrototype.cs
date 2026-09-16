using Content.Shared.Paper;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared._RedStar.Paperwork;

/// <summary>
/// Describes a paperwork document that can be rendered and printed.
/// </summary>
[Prototype]
public sealed partial class PaperworkPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// The optional category under which this document is displayed.
    /// </summary>
    [DataField]
    public ProtoId<PaperworkCategoryPrototype>? Category { get; private set; }

    /// <summary>
    /// The localized title of this document.
    /// </summary>
    [DataField(required: true)]
    public LocId Name { get; private set; }

    /// <summary>
    /// The resource path to the template used to render this document.
    /// </summary>
    [DataField(required: true)]
    public ResPath Template { get; private set; }

    /// <summary>
    /// The paper entity prototype used when this document is printed.
    /// </summary>
    [DataField(required: true)]
    public EntProtoId<PaperComponent> PaperPrototype { get; private set; }
}
