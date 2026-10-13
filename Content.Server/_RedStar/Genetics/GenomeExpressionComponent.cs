using Content.Shared._RedStar.Xenobiology;
using Robust.Shared.Prototypes;

namespace Content.Server._RedStar.Genetics;

/// <summary>
/// Tracks phenotype effects already applied by genetic expression.
/// Removing a gene does not undo permanent consequences.
/// </summary>
[RegisterComponent, Access(typeof(GenomeExpressionSystem))]
public sealed partial class GenomeExpressionComponent : Component
{
    [DataField]
    public HashSet<ProtoId<CellTraitPrototype>> Applied = [];
}
