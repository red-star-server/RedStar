using System.Linq;
using Content.Server._RedStar.Xenobiology.Components;
using Content.Server._RedStar.Xenobiology.Events;
using Content.Server.Research.Systems;
using Content.Shared._RedStar.Xenobiology.Components;
using Content.Shared.Research.Components;
using Robust.Shared.Prototypes;

namespace Content.Server._RedStar.Xenobiology.Systems;

/// <summary>
/// Catalogues physical samples and awards research for the first discovery on a server.
/// </summary>
public sealed partial class XenobiologyResearchSystem : EntitySystem
{
    [Dependency] private ResearchSystem _research = default!;
    [Dependency] private EntityQuery<XenobiologyResearchDatabaseComponent> _databaseQuery;

    public bool IsDiscovered(Entity<XenobiologyResearchDatabaseComponent?> server,
        EntProtoId<XenobiologySampleComponent> sample)
        => _databaseQuery.Resolve(server, ref server.Comp, false) && server.Comp.DiscoveredSamples.Contains(sample);

    public EntProtoId<XenobiologySampleComponent>[] GetDiscoveredSamples(Entity<XenobiologyResearchDatabaseComponent?> server)
        => _databaseQuery.Resolve(server, ref server.Comp, false) ? server.Comp.DiscoveredSamples.ToArray() : [];

    public bool TryAnalyzeSample(Entity<XenobiologyResearchDatabaseComponent?> server,
        EntProtoId<XenobiologySampleComponent> sample, out int reward)
    {
        reward = 0;
        if (!_databaseQuery.Resolve(server, ref server.Comp, false) ||
            !sample.TryGet(out var prototype, ProtoMan, Factory) ||
            !server.Comp.DiscoveredSamples.Add(sample))
            return false;

        reward = Math.Max(0, prototype.ResearchValue);
        _research.ModifyServerPoints(server.Owner, reward);
        if (!TryComp<ResearchServerComponent>(server.Owner, out var researchServer))
            return true;

        var ev = new XenobiologyResearchDatabaseChangedEvent();
        foreach (var client in researchServer.Clients)
        {
            RaiseLocalEvent(client, ref ev);
        }
        return true;
    }
}
