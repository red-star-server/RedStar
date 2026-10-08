using Robust.Shared.Prototypes;
using Content.Shared.EntityEffects;

namespace Content.Shared._RedStar.Xenobiology;

/// <summary>
/// Describes a biological cell trait and the permanent effects applied when it is gained.
/// </summary>
[Prototype]
public sealed partial class CellTraitPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; set; } = string.Empty;

    /// <summary>
    /// This trait has an obtainable biopsy or an initial cell sample and may be used in fusion recipes.
    /// </summary>
    [DataField]
    public bool FusionIngredient;

    /// <summary>
    /// May be produced by a fusion recipe instead of appearing as an ingredient.
    /// </summary>
    [DataField]
    public bool FusionMutation;

    [DataField]
    public LocId Name;

    [DataField]
    public Color Color;

    [DataField]
    public EntityEffect[] Effects = [];
}
