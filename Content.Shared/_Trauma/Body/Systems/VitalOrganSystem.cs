using Content.Shared._Trauma.DelayedDeath;
using Content.Shared.Body;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared._Trauma.Body;

/// <summary>
/// Handles vital-organ removal and reinsertion for organs marked with <see cref="VitalOrganComponent"/>.
/// When a vital organ is removed from a living body, <see cref="DelayedDeathComponent"/> is added.
/// When it is reinserted and the body still has a brain, the component is removed.
/// </summary>
public sealed partial class VitalOrganSystem : EntitySystem
{
    [Dependency] private BodySystem _body = default!;
    [Dependency] private IGameTiming _timing = default!;

    private static readonly ProtoId<OrganCategoryPrototype> Brain = "Brain";

    [SubscribeLocalEvent]
    private void OnRemoved(Entity<VitalOrganComponent> ent, ref OrganGotRemovedEvent args)
    {
        if (TerminatingOrDeleted(args.Target) || _timing.ApplyingState)
            return;

        var death = EnsureComp<DelayedDeathComponent>(args.Target);
        var key = GetKey(ent.Owner);
        death.MissingOrgans[key] = death.MissingOrgans.GetValueOrDefault(key) + 1;
        Dirty(args.Target, death);
    }

    [SubscribeLocalEvent]
    private void OnInserted(Entity<VitalOrganComponent> ent, ref OrganGotInsertedEvent args)
    {
        if (_timing.ApplyingState || TerminatingOrDeleted(ent) || TerminatingOrDeleted(args.Target) ||
            !TryComp<DelayedDeathComponent>(args.Target, out var death))
            return;

        var key = GetKey(ent.Owner);
        if (death.MissingOrgans.TryGetValue(key, out var count))
        {
            if (count > 1)
                death.MissingOrgans[key] = count - 1;
            else
                death.MissingOrgans.Remove(key);

            Dirty(args.Target, death);
        }

        TryClearDelayedDeath((args.Target, death));
    }

    /// <summary>
    /// Removes <see cref="DelayedDeathComponent"/> if no vital organ is missing and the body has a brain.
    /// </summary>
    public bool TryClearDelayedDeath(Entity<DelayedDeathComponent?> body)
    {
        if (!Resolve(body, ref body.Comp, false) ||
            _body.GetOrgan(body.Owner, Brain) == null)
            return false;

        if (body.Comp.MissingOrgans.Remove(CategoryKey(Brain)))
            Dirty(body);

        if (body.Comp.MissingOrgans.Count > 0)
            return false;

        RemComp(body.Owner, body.Comp);
        return true;
    }

    private static string CategoryKey(ProtoId<OrganCategoryPrototype> category) => $"category:{category.Id}";

    private string GetKey(EntityUid organ)
    {
        if (_body.GetCategory(organ) is { } category)
            return CategoryKey(category);

        return MetaData(organ).EntityPrototype is { } proto ? $"prototype:{proto.ID}" : $"entity:{organ}";
    }
}
