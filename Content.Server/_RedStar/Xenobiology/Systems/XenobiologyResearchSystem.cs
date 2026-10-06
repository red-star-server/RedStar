using System.Linq;
using Content.Server._RedStar.Xenobiology.Components;
using Content.Server._RedStar.Xenobiology.Events;
using Content.Server.Research.Systems;
using Content.Shared._RedStar.Xenobiology.Components;
using Content.Shared._RedStar.Xenobiology.Prototypes;
using Content.Shared.Research.Components;
using Robust.Shared.Prototypes;

namespace Content.Server._RedStar.Xenobiology.Systems;

public sealed partial class XenobiologyResearchSystem : EntitySystem
{
    [Dependency] private ResearchSystem _research = default!;

    private Dictionary<EntProtoId<XenobiologySampleComponent>, XenobiologyResearchPrototype>? _researchBySample;

    [SubscribeLocalEvent]
    private void OnPrototypesReloaded(PrototypesReloadedEventArgs args)
    {
        if (args.WasModified<XenobiologyResearchPrototype>() || args.WasModified<EntityPrototype>())
            _researchBySample = null;
    }

    private void EnsureResearchLookup()
    {
        if (_researchBySample != null)
            return;

        var lookup = new Dictionary<EntProtoId<XenobiologySampleComponent>, XenobiologyResearchPrototype>();
        foreach (var research in ProtoMan.EnumeratePrototypes<XenobiologyResearchPrototype>())
        {
            if (research.Sample.TryGet(out _, ProtoMan, Factory))
                lookup.Add(research.Sample, research);
        }

        _researchBySample = lookup;
    }

    public bool TryGetResearchPrototypeForSample(EntProtoId<XenobiologySampleComponent> sample,
        out XenobiologyResearchPrototype research)
    {
        EnsureResearchLookup();
        return _researchBySample!.TryGetValue(sample, out research!);
    }

    public bool IsDiscovered(Entity<XenobiologyResearchDatabaseComponent?> server,
        ProtoId<XenobiologyResearchPrototype> research)
    {
        return Resolve(server, ref server.Comp, false) &&
               server.Comp is { } database && database.DiscoveredSamples.Contains(research);
    }

    public ProtoId<XenobiologyResearchPrototype>[] GetDiscoveredSamples(Entity<XenobiologyResearchDatabaseComponent?> server)
    {
        return Resolve(server, ref server.Comp, false) && server.Comp is { } database
            ? database.DiscoveredSamples.ToArray()
            : [];
    }

    public int GetResearchReward(Entity<XenobiologyResearchDatabaseComponent?> server,
        XenobiologyResearchPrototype research)
    {
        if (!Resolve(server, ref server.Comp, false) || server.Comp is not { } database)
            return 0;

        if (!database.DiscoveredSamples.Contains(research.ID))
            return research.Reward;

        return (int) MathF.Floor(research.Reward * Math.Clamp(database.RepeatRewardMultiplier, 0f, 1f));
    }

    public bool TryAnalyzeSample(Entity<XenobiologyResearchDatabaseComponent?> server,
        EntProtoId<XenobiologySampleComponent> sample,
        out int reward)
    {
        reward = 0;
        if (!Resolve(server, ref server.Comp, false) || server.Comp is not { } database ||
            !TryGetResearchPrototypeForSample(sample, out var research))
            return false;

        reward = GetResearchReward(server, research);
        var discovered = database.DiscoveredSamples.Add(research.ID);
        _research.ModifyServerPoints(server.Owner, reward);
        if (!discovered || !TryComp<ResearchServerComponent>(server.Owner, out var researchServer))
            return true;

        var ev = new XenobiologyResearchDatabaseChangedEvent();
        foreach (var client in researchServer.Clients)
        {
            RaiseLocalEvent(client, ref ev);
        }

        return true;
    }
}
