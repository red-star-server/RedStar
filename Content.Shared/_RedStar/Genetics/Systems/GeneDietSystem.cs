using System.Linq;
using Content.Shared._RedStar.Genetics.Components;
using Content.Shared.Body;
using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;
using Content.Shared.Metabolism;
using Content.Shared.StatusEffectNew;
using Content.Shared.StatusEffectNew.Components;
using Content.Shared.Tag;
using Content.Shared.Whitelist;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;

namespace Content.Shared._RedStar.Genetics.Systems;

public sealed partial class GeneDietSystem : EntitySystem
{
    [Dependency] private INetManager _net = default!;
    [Dependency] private BodySystem _body = default!;
    [Dependency] private StomachSystem _stomach = default!;
    [Dependency] private MetabolizerSystem _metabolism = default!;

    private readonly HashSet<EntityUid> _pending = [];

    [SubscribeLocalEvent]
    private void OnApplied(Entity<GeneDietComponent> ent, ref StatusEffectAppliedEvent args) => QueueRefresh(args.Target);

    [SubscribeLocalEvent]
    private void OnRemoved(Entity<GeneDietComponent> ent, ref StatusEffectRemovedEvent args) => QueueRefresh(args.Target);

    [SubscribeLocalEvent]
    private void OnOrganInserted(Entity<GenomeComponent> ent, ref OrganInsertedIntoEvent args) => QueueRefresh(ent);

    [SubscribeLocalEvent]
    private void OnOrganRemoved(Entity<GeneDietOrganComponent> ent, ref OrganGotRemovedEvent args)
    {
        if (_net.IsServer)
            RemComp<GeneDietOrganComponent>(ent);
    }

    [SubscribeLocalEvent]
    private void OnState(Entity<GeneDietOrganComponent> ent, ref AfterAutoHandleStateEvent args) => Apply(ent);

    [SubscribeLocalEvent]
    private void OnStomachStartup(Entity<StomachComponent> ent, ref ComponentStartup args) => RefreshOrgan(ent);

    [SubscribeLocalEvent]
    private void OnStomachShutdown(Entity<StomachComponent> ent, ref ComponentShutdown args)
    {
        if (_net.IsServer)
            RemComp<GeneDietOrganComponent>(ent);
    }

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

    private void QueueRefresh(EntityUid body)
    {
        if (_net.IsServer)
            _pending.Add(body);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        if (_pending.Count == 0)
            return;

        var pending = _pending.ToArray();
        _pending.Clear();
        foreach (var body in pending)
        {
            if (!TerminatingOrDeleted(body))
                Refresh(body);
        }
    }

    private void Refresh(EntityUid body)
    {
        var profiles = new List<GeneDietComponent>();
        if (TryComp<StatusEffectContainerComponent>(body, out var container))
        {
            foreach (var uid in container.ActiveStatusEffects?.ContainedEntities ?? [])
            {
                if (!TerminatingOrDeleted(uid) && TryComp<GeneDietComponent>(uid, out var diet) &&
                    TryComp<StatusEffectComponent>(uid, out var status) && status.Applied)
                    profiles.Add(diet);
            }
        }

        foreach (var organ in _body.EnumerateOrgans<StomachComponent>(body))
        {
            if (profiles.Count == 0)
            {
                RemComp<GeneDietOrganComponent>(organ.Owner);
                continue;
            }

            if (!TryComp<GeneDietOrganComponent>(organ.Owner, out var state))
            {
                state = AddComp<GeneDietOrganComponent>(organ.Owner);
                state.OriginalWhitelist = organ.Comp2.SpecialDigestible;
                state.OriginalExclusive = organ.Comp2.IsSpecialDigestibleExclusive;
            }

            var allowNormal = profiles.Any(p => p.AllowNormalFood);
            var restrictions = profiles.Where(p => p.RestrictedFoodTags != null).ToArray();
            var additional = profiles.SelectMany(p => p.AdditionalFoodTags).Distinct().ToList();
            var whitelist = state.OriginalWhitelist;
            var exclusive = state.OriginalExclusive && state.OriginalWhitelist != null;
            if (restrictions.Length > 0 && !allowNormal)
            {
                var tags = new HashSet<ProtoId<TagPrototype>>(restrictions[0].RestrictedFoodTags!);
                foreach (var restriction in restrictions.Skip(1))
                {
                    tags.IntersectWith(restriction.RestrictedFoodTags!);
                }

                whitelist = new EntityWhitelist { Tags = tags.ToList() };
                exclusive = true;
            }
            else if (allowNormal)
                exclusive = false;

            if (additional.Count > 0)
            {
                // Conjunctive native whitelists cannot be represented as an OR with additional tags.
                // Preserve their original rule instead of silently weakening it.
                if (whitelist?.RequireAll != true)
                {
                    whitelist = new EntityWhitelist
                    {
                        Tags = (whitelist?.Tags ?? []).Concat(additional).Distinct().ToList(),
                        Components = whitelist?.Components,
                        Sizes = whitelist?.Sizes,
                    };
                }
            }

            state.Whitelist = whitelist;
            state.Exclusive = exclusive;
            state.MetabolizerTypes = profiles.SelectMany(p => p.MetabolizerTypes).ToHashSet();
            Apply((organ.Owner, state));
            Dirty(organ.Owner, state);
        }
    }

    private void Apply(Entity<GeneDietOrganComponent> ent)
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
