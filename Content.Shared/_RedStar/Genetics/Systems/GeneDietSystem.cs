using System.Linq;
using Content.Shared._RedStar.Genetics.Components;
using Content.Shared.Body;
using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;
using Content.Shared.Metabolism;
using Content.Shared.StatusEffectNew;

namespace Content.Shared._RedStar.Genetics.Systems;

public abstract partial class GeneDietSystem : EntitySystem
{
    [Dependency] private StomachSystem _stomach = default!;
    [Dependency] private MetabolizerSystem _metabolism = default!;

    [SubscribeLocalEvent]
    private void OnApplied(Entity<GeneDietComponent> ent, ref StatusEffectAppliedEvent args) => QueueRefresh(args.Target);

    [SubscribeLocalEvent]
    private void OnRemoved(Entity<GeneDietComponent> ent, ref StatusEffectRemovedEvent args) => QueueRefresh(args.Target);

    [SubscribeLocalEvent]
    private void OnOrganInserted(Entity<GenomeComponent> ent, ref OrganInsertedIntoEvent args) => QueueRefresh(ent);

    [SubscribeLocalEvent]
    private void OnOrganRemoved(Entity<GeneDietOrganComponent> ent, ref OrganGotRemovedEvent args) => RemoveOrganState(ent);

    [SubscribeLocalEvent]
    private void OnState(Entity<GeneDietOrganComponent> ent, ref AfterAutoHandleStateEvent args) => Apply(ent);

    [SubscribeLocalEvent]
    private void OnStomachStartup(Entity<StomachComponent> ent, ref ComponentStartup args) => RefreshOrgan(ent);

    [SubscribeLocalEvent]
    private void OnStomachShutdown(Entity<StomachComponent> ent, ref ComponentShutdown args) => RemoveOrganState(ent);

    [SubscribeLocalEvent]
    private void OnMetabolizerStartup(Entity<MetabolizerComponent> ent, ref ComponentStartup args) => RefreshOrgan(ent);

    [SubscribeLocalEvent]
    private void OnMetabolizerShutdown(Entity<MetabolizerComponent> ent, ref ComponentShutdown args)
    {
        if (TryComp<GeneDietOrganComponent>(ent, out var diet))
            diet.AppliedMetabolizers.Clear();
    }

    private void RefreshOrgan(EntityUid organ)
    {
        if (TryComp<GeneDietOrganComponent>(organ, out var diet))
            Apply((organ, diet));
        if (TryComp<OrganComponent>(organ, out var attached) && attached.Body is { } body)
            QueueRefresh(body);
    }

    [SubscribeLocalEvent]
    private void OnShutdown(Entity<GeneDietOrganComponent> ent, ref ComponentShutdown args)
    {
        if (TerminatingOrDeleted(ent))
            return;

        _stomach.SetDiet(ent.Owner, ent.Comp.OriginalWhitelist, ent.Comp.OriginalExclusive);
        foreach (var type in ent.Comp.AppliedMetabolizers)
        {
            _metabolism.TryRemoveMetabolizerType(ent.Owner, type);
        }
    }

    protected virtual void QueueRefresh(EntityUid body) { }

    protected virtual void RemoveOrganState(EntityUid organ) { }

    protected void Apply(Entity<GeneDietOrganComponent> ent)
    {
        _stomach.SetDiet(ent.Owner, ent.Comp.Whitelist, ent.Comp.Exclusive);
        foreach (var type in ent.Comp.AppliedMetabolizers.ToArray())
        {
            if (ent.Comp.MetabolizerTypes.Contains(type))
                continue;
            _metabolism.TryRemoveMetabolizerType(ent.Owner, type);
            ent.Comp.AppliedMetabolizers.Remove(type);
        }
        foreach (var type in ent.Comp.MetabolizerTypes)
        {
            if (_metabolism.TryAddMetabolizerType(ent.Owner, type))
                ent.Comp.AppliedMetabolizers.Add(type);
        }
    }
}
