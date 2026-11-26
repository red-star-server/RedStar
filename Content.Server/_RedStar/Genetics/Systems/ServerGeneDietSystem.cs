using System.Linq;
using Content.Shared._RedStar.Genetics.Components;
using Content.Shared._RedStar.Genetics.Systems;
using Content.Shared.Body;
using Content.Shared.Body.Components;
using Content.Shared.StatusEffectNew.Components;
using Content.Shared.Tag;
using Content.Shared.Whitelist;
using Robust.Shared.Prototypes;

namespace Content.Server._RedStar.Genetics.Systems;

public sealed partial class ServerGeneDietSystem : GeneDietSystem
{
    [Dependency] private BodySystem _body = default!;

    private readonly HashSet<EntityUid> _pending = [];

    protected override void QueueRefresh(EntityUid body) => _pending.Add(body);

    protected override void RemoveOrganState(EntityUid organ) => RemComp<GeneDietOrganComponent>(organ);

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
                        Sizes = whitelist?.Sizes
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
}
