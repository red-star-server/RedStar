using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared._RedStar.Paperwork;

/// <summary>
/// A category used to group paperwork forms.
/// </summary>
[Prototype]
public sealed partial class PaperworkCategoryPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// The localized name displayed for this category.
    /// </summary>
    [DataField(required: true)]
    public LocId Name { get; private set; }

    /// <summary>
    /// An optional icon displayed alongside the category name.
    /// </summary>
    [DataField]
    public SpriteSpecifier? Icon { get; private set; }
}
