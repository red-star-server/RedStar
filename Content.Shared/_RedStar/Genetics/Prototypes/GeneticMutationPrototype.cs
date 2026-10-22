using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Shared._RedStar.Genetics.Prototypes;

[Prototype]
public sealed partial class GeneticMutationPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = string.Empty;

    /// <summary>
    /// Dormant genes consumed together when this mutation completes.
    /// </summary>
    [DataField(required: true)]
    public HashSet<ProtoId<GenePrototype>> RequiredGenes = [];

    /// <summary>
    /// Reagent amounts required throughout processing and consumed when the mutation completes.
    /// </summary>
    [DataField]
    public Dictionary<ProtoId<ReagentPrototype>, FixedPoint2> RequiredReagents = [];

    /// <summary>
    /// Gene replacing the inputs. It must be absent from the subject before processing.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<GenePrototype> ResultGene;

    /// <summary>
    /// Higher values take precedence when several recipes match the same subject and mixture.
    /// Equal priorities are resolved by prototype ID in ordinal order.
    /// </summary>
    [DataField]
    public int Priority;

    [DataField]
    public TimeSpan Duration = TimeSpan.FromSeconds(60);
}
