using Content.Shared.Metabolism;
using Content.Shared.Tag;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RedStar.Genetics.Components;

/// <summary>
/// Dietary contributions supplied by a gene status to the host's stomachs.
/// Ordinary-food permission takes precedence over acquired restrictions; extra foods remain additive.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class GeneDietComponent : Component
{
    /// <summary>
    /// Permits ordinary food while retaining the stomach's special-food whitelist.
    /// </summary>
    [DataField]
    public bool AllowNormalFood;

    /// <summary>
    /// Restricts ordinary food to these tags unless another active gene grants ordinary food.
    /// </summary>
    [DataField]
    public List<ProtoId<TagPrototype>>? RestrictedFoodTags;

    /// <summary>
    /// Additional special foods admitted by this gene, independently of ordinary-food restrictions.
    /// </summary>
    [DataField]
    public List<ProtoId<TagPrototype>> AdditionalFoodTags = [];

    /// <summary>
    /// Additional metabolism types granted only to stomach organs, not to the liver or lungs.
    /// </summary>
    [DataField]
    public HashSet<ProtoId<MetabolizerTypePrototype>> MetabolizerTypes = [];
}
