using Content.Server._RedStar.Genetics.Systems;
using Content.Shared._RedStar.Genetics.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Server._RedStar.Genetics.Components;

[RegisterComponent]
[Access(typeof(GeneResearchSystem))]
public sealed partial class GeneResearchDatabaseComponent : Component
{
    /// <summary>
    /// Genes successfully sequenced at least once for this research server.
    /// </summary>
    [DataField]
    public HashSet<ProtoId<GenePrototype>> DiscoveredGenes = new();
}