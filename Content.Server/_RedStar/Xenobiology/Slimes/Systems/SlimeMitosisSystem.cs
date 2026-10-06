using Content.Server._RedStar.Xenobiology.Slimes.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Nutrition.AnimalHusbandry;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using Robust.Shared.Map;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._RedStar.Xenobiology.Slimes.Systems;

/// <summary>
/// Handles slime-specific partnerless division and removes the adult after birth.
/// </summary>
public sealed partial class SlimeMitosisSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private SatiationSystem _satiation = default!;
    [Dependency] private SlimeMutationSystem _mutation = default!;
    [Dependency] private SharedTransformSystem _transform = default!;

    [Dependency] private EntityQuery<SlimeDigestionComponent> _digestionQuery;
    [Dependency] private EntityQuery<MobStateComponent> _mobQuery;
    [Dependency] private EntityQuery<SlimeMutationComponent> _mutationQuery;

    [SubscribeLocalEvent]
    private void OnMapInit(Entity<SlimeMitosisComponent> ent, ref MapInitEvent args)
        => ent.Comp.NextAttempt = _timing.CurTime + _random.Next(ent.Comp.MinInterval, ent.Comp.MaxInterval);

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<SlimeMitosisComponent>();
        while (query.MoveNext(out var uid, out var mitosis))
        {
            if (!_mobQuery.TryComp(uid, out var state) || state.CurrentState != MobState.Alive || IsFeeding(uid))
                continue;
            if (mitosis.GestationEnd is { } end)
            {
                if (_timing.CurTime >= end)
                    Divide((uid, mitosis));
                continue;
            }
            if (_timing.CurTime < mitosis.NextAttempt || HasComp<InfantComponent>(uid))
                continue;
            if (TryComp<SatiationComponent>(uid, out var satiation))
                _satiation.ModifyValue((uid, satiation), SatiationSystem.Hunger, -mitosis.HungerPerBirth);
            mitosis.GestationEnd = _timing.CurTime + mitosis.GestationDuration;
        }
    }

    private bool IsFeeding(EntityUid uid)
        => _digestionQuery.TryComp(uid, out var digestion) &&
           digestion.ConsumeDoAfter != null;

    private void Divide(Entity<SlimeMitosisComponent> ent)
    {
        if (!_mutationQuery.TryComp(ent.Owner, out var mutation) || IsFeeding(ent.Owner) ||
            !_transform.TryGetMapOrGridCoordinates(ent.Owner, out var coordinates))
            return;
        for (var i = 0; i < ent.Comp.OffspringCount; i++)
        {
            var childCoordinates = new EntityCoordinates(
                coordinates.Value.EntityId,
                coordinates.Value.Position + _random.NextVector2(0.3f));
            Spawn(_mutation.ResolveOffspring((ent.Owner, mutation), ent.Comp.OffspringPrototype), childCoordinates);
        }

        QueueDel(ent.Owner);
    }
}
