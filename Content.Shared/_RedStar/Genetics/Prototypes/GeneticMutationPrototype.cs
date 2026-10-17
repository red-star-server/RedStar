using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Shared._RedStar.Genetics.Prototypes;

[Prototype]
public sealed partial class GeneticMutationPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = string.Empty;

    [DataField(required: true)]
    public HashSet<ProtoId<GenePrototype>> RequiredGenes = [];

    [DataField]
    public Dictionary<ProtoId<ReagentPrototype>, FixedPoint2> RequiredReagents = [];

    [DataField(required: true)]
    public ProtoId<GenePrototype> ResultGene;

    [DataField]
    public TimeSpan Duration = TimeSpan.FromSeconds(60);
}
