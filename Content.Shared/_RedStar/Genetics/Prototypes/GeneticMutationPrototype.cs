using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Shared._RedStar.Genetics.Prototypes;

/// <summary>
/// Genetic mutation recipe. Machines provide a suitable environment; the recipe defines
/// which genes and reagent concentrations are needed and what new presence gene can appear.
/// </summary>
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
    public float Chance = 1f;
}
