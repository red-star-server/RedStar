using Content.Server.Research.Systems;
using Content.Shared._RedStar.Xenobiology;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._RedStar.Xenobiology;

public sealed partial class XenobiologyResearchSystem : EntitySystem
{
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private ResearchSystem _research = default!;

    [SubscribeLocalEvent]
    private void OnMapInit(Entity<XenobiologyResearchDatabaseComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.ActiveTargets.RemoveAll(id =>
            ent.Comp.CompletedTargets.Contains(id) ||
            !ProtoMan.TryIndex(id, out var target) ||
            !ValidReferences(target));
        FillTargets(ent.Comp);
    }

    public bool HasActiveSample(EntityUid server, EntProtoId<SlimeExtractComponent> sample)
    {
        if (!TryComp<XenobiologyResearchDatabaseComponent>(server, out var database))
            return false;

        foreach (var id in database.ActiveTargets)
        {
            if (!database.CompletedTargets.Contains(id) &&
                ProtoMan.TryIndex(id, out var target) &&
                target.Sample == sample)
                return true;
        }

        return false;
    }

    public bool TryCompleteSample(EntityUid server, EntProtoId<SlimeExtractComponent> sample, out int reward)
    {
        reward = 0;
        if (!TryComp<XenobiologyResearchDatabaseComponent>(server, out var database))
            return false;

        for (var i = 0; i < database.ActiveTargets.Count; i++)
        {
            var id = database.ActiveTargets[i];
            if (database.CompletedTargets.Contains(id) ||
                !ProtoMan.TryIndex(id, out var target) ||
                target.Sample != sample)
                continue;

            reward = target.Reward;
            database.CompletedTargets.Add(id);
            database.ActiveTargets.RemoveAt(i);
            FillTargets(database);
            _research.ModifyServerPoints(server, reward);
            return true;
        }

        return false;
    }

    private void FillTargets(XenobiologyResearchDatabaseComponent database)
    {
        while (database.ActiveTargets.Count < database.ActiveTargetCount && TryPickTarget(database, out var id))
        {
            database.ActiveTargets.Add(id);
        }
    }

    private bool TryPickTarget(XenobiologyResearchDatabaseComponent database,
        out ProtoId<XenobiologyResearchPrototype> selected)
    {
        selected = default;
        var candidates = new List<XenobiologyResearchPrototype>();
        var totalWeight = 0f;

        foreach (var target in ProtoMan.EnumeratePrototypes<XenobiologyResearchPrototype>())
        {
            if (database.CompletedTargets.Contains(target.ID) || database.ActiveTargets.Contains(target.ID) ||
                target.Weight <= 0 || target.Reward <= 0 || !ValidReferences(target))
                continue;

            var unlocked = true;
            foreach (var prerequisite in target.Prerequisites)
            {
                if (database.CompletedTargets.Contains(prerequisite))
                    continue;

                unlocked = false;
                break;
            }

            if (!unlocked)
                continue;

            candidates.Add(target);
            totalWeight += target.Weight;
        }

        if (candidates.Count == 0)
            return false;

        var roll = _random.NextFloat() * totalWeight;
        foreach (var candidate in candidates)
        {
            roll -= candidate.Weight;
            if (roll >= 0)
                continue;

            selected = candidate.ID;
            return true;
        }

        selected = candidates[^1].ID;
        return true;
    }

    private bool ValidReferences(XenobiologyResearchPrototype target)
    {
        if (!target.Sample.TryGet(out _, ProtoMan, Factory))
            return false;

        foreach (var prerequisite in target.Prerequisites)
        {
            if (!ProtoMan.TryIndex(prerequisite, out _))
                return false;
        }

        return true;
    }
}
