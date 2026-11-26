using Content.Server._RedStar.Genetics.Components;
using Content.Server.Research.Systems;
using Content.Shared._RedStar.Genetics.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Server._RedStar.Genetics.Systems;

public sealed partial class GeneResearchSystem : EntitySystem
{
    [Dependency] private IPrototypeManager _prototypes = default!;
    [Dependency] private ResearchSystem _research = default!;

    /// <summary>
    /// Records a newly sequenced gene on the connected research server and awards its one-time research points.
    /// </summary>
    public bool TryRegisterDiscovery(EntityUid client, ProtoId<GenePrototype> gene, out int points)
    {
        points = 0;
        if (!_prototypes.TryIndex(gene, out var prototype) ||
            !_research.TryGetClientServer(client, out var server, out var researchServer) ||
            !TryComp<GeneResearchDatabaseComponent>(server.Value, out var database) ||
            !database.DiscoveredGenes.Add(gene))
            return false;

        points = GetResearchPoints(prototype.Tier);
        _research.ModifyServerPoints(server.Value, points, researchServer);
        return true;
    }

    private static int GetResearchPoints(int tier) => tier switch
    {
        1 => 500,
        2 => 1000,
        3 => 2500,
        4 => 5000,
        _ => 0
    };
}
