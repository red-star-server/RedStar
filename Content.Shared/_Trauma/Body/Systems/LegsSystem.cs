using Content.Shared._Trauma.Common.Traumas;
using Content.Shared.Body;
using Content.Shared.Movement.Systems;
using Content.Shared.Pulling.Events;
using Content.Shared.Standing;
using Content.Shared._Trauma.Traumas;

namespace Content.Shared._Trauma.Body;

public sealed partial class LegsSystem : EntitySystem
{
    [Dependency] private MovementSpeedModifierSystem _movement = default!;

    [Dependency] private EntityQuery<LegsComponent> _query;
    [Dependency] private EntityQuery<LegComponent> _legQuery;
    [Dependency] private EntityQuery<BoneComponent> _boneQuery;

    [SubscribeLocalEvent]
    private void OnStandAttempt(Entity<LegsComponent> ent, ref StandAttemptEvent args)
    {
        var usableLegs = 0;
        foreach (var leg in ent.Comp.Legs)
        {
            if (_boneQuery.TryComp(leg, out BoneComponent? bone)
                && bone.BoneSeverity == BoneSeverity.Broken)
                continue;

            usableLegs++;
        }

        if (usableLegs < ent.Comp.Required)
            args.Cancel();
    }

    [SubscribeLocalEvent]
    private void OnAttemptStopPulling(Entity<LegsComponent> ent, ref AttemptStopPullingEvent args) // Goobstation
    {
        if (args.User is not {} user || !Exists(user))
            return;

        if (user != ent.Owner)
            return;

        if (ent.Comp.Legs.Count > 0 || ent.Comp.Required == 0)
            return;

        args.Cancelled = true;
    }

    [SubscribeLocalEvent]
    private void OnLegAdded(Entity<LegComponent> ent, ref OrganGotInsertedEvent args)
    {
        if (!_query.TryComp(args.Target, out var comp))
            return;

        comp.Legs.Add(ent.Owner);
        Dirty(args.Target, comp);

        UpdateMovementSpeed((args.Target, comp));
    }

    [SubscribeLocalEvent]
    private void OnLegRemoved(Entity<LegComponent> ent, ref OrganGotRemovedEvent args)
    {
        if (TerminatingOrDeleted(args.Target) || !_query.TryComp(args.Target, out var comp))
            return;

        comp.Legs.Remove(ent.Owner);
        Dirty(args.Target, comp);

        UpdateMovementSpeed((args.Target, comp));
    }

    /// <summary>
    /// Recalculates movement speed from the currently attached legs.
    /// </summary>
    public void UpdateMovementSpeed(Entity<LegsComponent> ent)
    {
        var walkSpeed = 0f;
        var sprintSpeed = 0f;
        var acceleration = 0f;
        foreach (var leg in ent.Comp.Legs)
        {
            if (!_legQuery.TryComp(leg, out var comp))
                continue;

            walkSpeed += comp.WalkSpeed;
            sprintSpeed += comp.SprintSpeed;
            acceleration += comp.Acceleration;
        }

        // bare minimum speeds for crawling if you have no legs
        // could make it need arms too, but torsolo...
        walkSpeed = Math.Max(walkSpeed, 1f);
        sprintSpeed = Math.Max(sprintSpeed, 1f);
        acceleration = Math.Max(acceleration, 0.5f);

        // missing a leg makes you move at half speed
        // somehow having 3+ legs makes you fast
        var scale = ent.Comp.Required > 0 ? 1f / ent.Comp.Required : 1f;
        walkSpeed *= scale;
        sprintSpeed *= scale;
        acceleration *= scale;
        _movement.ChangeBaseSpeed(ent.Owner, walkSpeed, sprintSpeed, acceleration);
    }

}
