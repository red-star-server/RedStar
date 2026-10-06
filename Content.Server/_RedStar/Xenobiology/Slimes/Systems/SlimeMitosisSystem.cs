using Content.Server._RedStar.Xenobiology.Slimes.Components;
using Content.Shared.DoAfter;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Nutrition.AnimalHusbandry;

namespace Content.Server._RedStar.Xenobiology.Slimes.Systems;

/// <summary>
/// Defers division while feeding and removes the carrier after a completed birth.
/// </summary>
public sealed partial class SlimeMitosisSystem : EntitySystem
{
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private EntityQuery<SlimeDigestionComponent> _digestionQuery;
    [Dependency] private EntityQuery<MobStateComponent> _mobQuery;

    private bool IsFeeding(EntityUid uid)
    {
        return _digestionQuery.TryComp(uid, out var digestion) &&
               (digestion.Stomach.ContainedEntity != null || _doAfter.IsRunning(digestion.ConsumeDoAfter));
    }

    [SubscribeLocalEvent]
    private void OnReproductionAttempt(Entity<SlimeMitosisComponent> ent, ref ReproductionAttemptEvent args)
    {
        if (IsFeeding(ent.Owner) || !_mobQuery.TryComp(ent.Owner, out var state) || state.CurrentState != MobState.Alive)
            args.Cancel();
    }

    [SubscribeLocalEvent]
    private void OnBirthAttempt(Entity<SlimeMitosisComponent> ent, ref BirthAttemptEvent args)
    {
        if (IsFeeding(ent.Owner) || !_mobQuery.TryComp(ent.Owner, out var state) || state.CurrentState != MobState.Alive)
            args.Cancel();
    }

    [SubscribeLocalEvent]
    private void OnBirthCompleted(Entity<SlimeMitosisComponent> ent, ref BirthCompletedEvent args)
    {
        if (args.Offspring.Count == 0 || IsFeeding(ent.Owner))
            return;

        foreach (var offspring in args.Offspring)
        {
            if (TerminatingOrDeleted(offspring))
                return;
        }

        QueueDel(ent.Owner);
    }
}
