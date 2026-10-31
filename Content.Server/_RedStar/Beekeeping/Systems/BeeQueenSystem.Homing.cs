using System.Numerics;
using Content.Server._RedStar.Beekeeping.Components;
using Content.Server._RedStar.Beekeeping.Data;
using Content.Shared._RedStar.Beekeeping.Components;
using Content.Shared._RedStar.Beekeeping.Visuals;
using Content.Shared.Audio;
using Content.Shared.Throwing;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;

namespace Content.Server._RedStar.Beekeeping.Systems;

/// <summary>
/// A queen left on the floor flies home on her own (<see cref="BeeHomingComponent"/>) into the queen slot of her hive,
/// if it is free. Only a clear straight line counts, like for visits: no flying through walls. Worker bees and the scout
/// fly home as NPCs (<see cref="BeeErrandSystem.FlyHome"/>).
/// </summary>
public sealed partial class BeeQueenSystem
{
    [Dependency] private SharedPhysicsSystem _physics = default!;

    private void UpdateHoming(float frameTime)
    {
        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<BeeHomingComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var homing, out var xform))
        {
            // Held or packed away it buzzes quietly; free, it buzzes like a flying bee.
            var contained = _container.TryGetContainingContainer(uid, out var holder);
            UpdateBeeSound((uid, homing), !contained);

            // Inside a hive the colony buzzes for her: her own hand-sound would still be heard from there.
            var quiet = contained && HasComp<BeehiveComponent>(holder!.Owner);
            if (TryComp<AmbientSoundComponent>(uid, out var heard) && heard.Enabled == quiet)
                _ambient.SetAmbience(uid, !quiet);

            // In a hand, a bag or a locker, or still in the air after a throw: not on its own yet.
            if (contained || HasComp<ThrownItemComponent>(uid))
            {
                StopHoming((uid, homing));
                continue;
            }

            if (now >= homing.NextCheck)
            {
                homing.NextCheck = now + homing.CheckInterval;
                var home = GetHomingHive(uid);
                if (home is { } own && !CanFlyHome(uid, xform, homing, own))
                    home = null;

                // A queen with no hive of her own takes any empty one she can see.
                if (home == null && HasComp<BeeQueenComponent>(uid))
                    home = FindFreeHive(uid, xform, homing);

                if (home is not { } found)
                {
                    StopHoming((uid, homing));
                }
                else
                {
                    homing.Target = found;
                    if (!homing.Flying)
                        StartHoming((uid, homing));
                }
            }

            if (!homing.Flying ||
                homing.Target is not { } targetHive ||
                TerminatingOrDeleted(targetHive) ||
                !TryComp<BeehiveComponent>(targetHive, out var targetComp))
            {
                continue;
            }

            var hive = new Entity<BeehiveComponent>(targetHive, targetComp);

            var target = Transform(hive).LocalPosition;
            var delta = target - xform.LocalPosition;
            var distance = delta.Length();

            FaceHome((uid, homing), xform, delta);

            if (distance <= homing.Speed * frameTime)
            {
                if (TryComp<PhysicsComponent>(uid, out var snapBody))
                    _physics.SetLinearVelocity(uid, Vector2.Zero, body: snapBody);
                _transform.SetLocalPosition(uid, target, xform);
                StopHoming((uid, homing));
                homing.NextCheck = now + homing.CheckInterval;
                ArriveHome(uid, hive);
                continue;
            }

            if (TryComp<PhysicsComponent>(uid, out var body))
                _physics.SetLinearVelocity(uid, delta / distance * homing.Speed, body: body);
        }
    }

    /// <summary>
    /// Turns the flying bee towards its hive. Its sprite does not follow the spin of a throw (noRot for side
    /// views), so a mirrored one only needs to look left or right; a top-down one is pointed along its way.
    /// </summary>
    private void FaceHome(Entity<BeeHomingComponent> homing, TransformComponent xform, Vector2 localDirection)
    {
        switch (homing.Comp.Facing)
        {
            case BeeHomingFacing.Mirror:
                _errands.SetFacing(homing, localDirection);
                break;
            case BeeHomingFacing.Rotate:
                // The sprite looks up (+Y) at rotation zero.
                _transform.SetLocalRotation(homing, new Angle(MathF.Atan2(-localDirection.X, localDirection.Y)), xform);
                break;
        }
    }

    /// <summary>
    /// Switches between the quiet buzz of a held bee and the buzz of a free one, only when it changes.
    /// </summary>
    private void UpdateBeeSound(Entity<BeeHomingComponent> homing, bool free)
    {
        if (homing.Comp.FreeSound == free ||
            homing.Comp.FlyingSound is not { } flying ||
            !TryComp<AmbientSoundComponent>(homing, out var ambient))
        {
            return;
        }

        homing.Comp.RestingSound ??= ambient.Sound;
        homing.Comp.FreeSound = free;
        _ambient.SetSound(homing, free ? flying : homing.Comp.RestingSound, ambient);
    }

    /// <summary>
    /// Starts a flight. Whatever speed and spin a throw left on the body is dropped: the flight moves it.
    /// </summary>
    private void StartHoming(Entity<BeeHomingComponent> homing)
    {
        homing.Comp.Flying = true;
        _appearance.SetData(homing, BeeForagerVisuals.Flying, true);

        if (!TryComp<PhysicsComponent>(homing, out var body))
            return;

        _physics.SetLinearVelocity(homing, Vector2.Zero, body: body);
        _physics.SetAngularVelocity(homing, 0f, body: body);
    }

    /// <summary>
    /// Ends a flight; a mirrored sprite looks left again, so it is not shown flipped in a hand.
    /// </summary>
    private void StopHoming(Entity<BeeHomingComponent> homing)
    {
        if (!homing.Comp.Flying)
            return;

        homing.Comp.Flying = false;
        homing.Comp.Target = null;
        _appearance.SetData(homing, BeeForagerVisuals.Flying, false);
        if (homing.Comp.Facing == BeeHomingFacing.Mirror)
            _appearance.SetData(homing, BeeForagerVisuals.FacingRight, false);

        if (TryComp<PhysicsComponent>(homing, out var body))
            _physics.SetLinearVelocity(homing, Vector2.Zero, body: body);
    }

    /// <summary>
    /// The hive this queen would fly to, or null if there is none to go back to.
    /// </summary>
    private Entity<BeehiveComponent>? GetHomingHive(EntityUid uid)
    {
        EntityUid? hive = null;

        if (TryComp<BeeQueenComponent>(uid, out var queen))
            hive = queen.LastHive;

        if (hive is not { } hiveUid ||
            TerminatingOrDeleted(hiveUid) ||
            !TryComp<BeehiveComponent>(hiveUid, out var comp))
        {
            return null;
        }

        // A queen only goes back into a hive that has no queen.
        if (HasComp<BeeQueenComponent>(uid) && _itemSlots.GetItemOrNull(hiveUid, comp.QueenSlot) != null)
            return null;

        return (hiveUid, comp);
    }

    /// <summary>
    /// Both lie directly on the same grid, the hive is close enough and nothing blocks the straight line.
    /// </summary>
    private bool CanFlyHome(EntityUid uid, TransformComponent xform, BeeHomingComponent homing, EntityUid hive)
    {
        var hiveXform = Transform(hive);
        if (xform.GridUid is not { } grid || xform.ParentUid != grid || hiveXform.ParentUid != grid)
            return false;

        if (Vector2.Distance(xform.LocalPosition, hiveXform.LocalPosition) > homing.Range)
            return false;

        return _scout.ClearFlightPath(_transform.GetMapCoordinates(uid, xform), _transform.GetMapCoordinates(hive, hiveXform), uid, hive);
    }

    /// <summary>
    /// The nearest hive without a queen that she can fly to in a straight line.
    /// </summary>
    private readonly HashSet<Entity<BeehiveComponent>> _nearbyHives = new();

    private Entity<BeehiveComponent>? FindFreeHive(EntityUid queen, TransformComponent xform, BeeHomingComponent homing)
    {
        if (xform.GridUid is not { } grid || xform.ParentUid != grid)
            return null;

        Entity<BeehiveComponent>? best = null;
        var bestDistance = float.MaxValue;

        _nearbyHives.Clear();
        _lookup.GetEntitiesInRange(_transform.GetMapCoordinates(xform), homing.Range, _nearbyHives);

        foreach (var (uid, comp) in _nearbyHives)
        {
            var hiveXform = Transform(uid);
            if (hiveXform.ParentUid != grid ||
                _itemSlots.GetItemOrNull(uid, comp.QueenSlot) != null)
            {
                continue;
            }

            var distance = Vector2.Distance(xform.LocalPosition, hiveXform.LocalPosition);
            if (distance >= bestDistance || !CanFlyHome(queen, xform, homing, uid))
                continue;

            best = (uid, comp);
            bestDistance = distance;
        }

        return best;
    }

    private void ArriveHome(EntityUid uid, Entity<BeehiveComponent> hive)
    {
        if (HasComp<BeeQueenComponent>(uid))
            _itemSlots.TryInsert(hive.Owner, hive.Comp.QueenSlot, uid, null);
    }
}
