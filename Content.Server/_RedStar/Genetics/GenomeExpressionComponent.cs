using Content.Shared._RedStar.Xenobiology;
using Robust.Shared.Prototypes;

namespace Content.Server._RedStar.Genetics;

/// <summary>
/// Tracks phenotype effects applied by genetic expression.
/// Component ownership is recorded so removing a gene leaves unrelated components alone.
/// </summary>
[RegisterComponent, Access(typeof(GenomeExpressionSystem))]
public sealed partial class GenomeExpressionComponent : Component
{
    [DataField]
    public HashSet<ProtoId<CellTraitPrototype>> Applied = [];

    [DataField]
    public Dictionary<ProtoId<CellTraitPrototype>, HashSet<string>> OwnedComponents = [];
}
