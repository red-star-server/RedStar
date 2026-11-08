using System.Numerics;
using Content.Server._RedStar.Beekeeping.Components;
using Content.Server._RedStar.Beekeeping.Data;
using Content.Shared._RedStar.Beekeeping.Components;
using Content.Shared._RedStar.Beekeeping.Systems;
using Content.Shared.Physics;
using Content.Shared.Storage.Components;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Timing;

namespace Content.Server._RedStar.Beekeeping.Systems;

/// <summary>
/// The scout and the hive's known sources. When a trip is due the hive looks for trays with a living plant around it
/// (<see cref="EntityLookupSystem"/>, a clear straight line from the hive), and the scout flies to the first one it
/// found and back like a forager does; the hive learns the trays when it lands. The sources part keeps the known
/// trays checked and sends the foragers out. Finding trays is all here; the flight is the bee's own, an NPC errand (<see cref="BeeErrandSystem"/>).
/// </summary>
public sealed partial class BeeScoutSystem : EntitySystem
{
    [Dependency] private SharedPhysicsSystem _physics = default!;
    [Dependency] private BeeErrandSystem _errands = default!;
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private BeehiveSystem _hive = default!;
    [Dependency] private IGameTiming _timing = default!;

    private readonly List<EntityUid> _found = new();
    private readonly List<(EntityUid Tray, float Distance)> _candidates = new();

    /// <summary>
    /// While the scout is home it flies out every so often; the pause between trips starts when it is back. A scout
    /// that is taken out, lost or kept home by bad air is called back at once.
    /// </summary>
    public void UpdateScouting(Entity<BeehiveComponent> hive)
    {
        var comp = hive.Comp;
        var now = _timing.CurTime;

        if (SharedBeehiveSystem.ScoutsAtHome(comp) <= 0)
        {
            comp.NextScoutTrip = TimeSpan.Zero;
            RecallScout(comp);
            return;
        }

        if (ScoutFlying(comp))
            return;

        if (comp.NextScoutTrip == TimeSpan.Zero)
        {
            // The first trip of a new colony goes out within seconds; the normal pauses follow it.
            comp.NextScoutTrip = comp.ScoutFirstTripNow
                ? now + comp.FirstScoutDelay
                : now + RandomTrip(comp, _hive.GetHiveTotals(hive).ScoutPause);
            comp.ScoutFirstTripNow = false;
            return;
        }

        if (now < comp.NextScoutTrip)
            return;

        // The pause starts over once this trip is done. Nothing new in sight: try again after a pause.
        comp.NextScoutTrip = ScoutTrip(hive) ? TimeSpan.Zero : now + RandomTrip(comp);
    }

    private TimeSpan RandomTrip(BeehiveComponent comp, float pause = 1f)
    {
        var min = comp.ScoutTripMin.TotalSeconds;
        var max = Math.Max(min, comp.ScoutTripMax.TotalSeconds);
        return TimeSpan.FromSeconds((min + _random.NextDouble() * (max - min)) * pause);
    }

    /// <summary>
    /// The scout looks for trays it does not know yet and flies to the first of them. Returns false if it found none.
    /// </summary>
    public bool ScoutTrip(Entity<BeehiveComponent> hive)
    {
        var finds = Math.Clamp(hive.Comp.ScoutFinds + _hive.GetHiveTotals(hive).ScoutFinds, 1, 5);
        FindNewTrays(hive, finds, _found);
        if (_found.Count == 0)
            return false;

        // With nobody around to watch the trip, the hive simply learns what the scout found.
        var xform = Transform(hive);
        if (!_errands.SomeoneWatching(xform.Coordinates))
        {
            Learn(hive, _found);
            return false;
        }

        return _errands.TrySpawnScout(hive, xform, _found);
    }

    /// <summary>
    /// Up to <paramref name="max"/> trays with a living plant within <see cref="BeehiveComponent.ScoutRange"/> that the
    /// hive does not know yet and could visit: on its grid, not inside anything and in a clear straight line. The nearest
    /// come first, so the scout's trip is short.
    /// </summary>
    public void FindNewTrays(Entity<BeehiveComponent> hive, int max, List<EntityUid> found)
    {
        found.Clear();
        var comp = hive.Comp;
        var xform = Transform(hive);
        if (xform.GridUid is not { } grid)
            return;

        var hiveMap = _transform.GetMapCoordinates(hive, xform);
        _foundTrays.Clear();
        _lookup.GetEntitiesInRange(hiveMap, comp.ScoutRange, _foundTrays);

        _candidates.Clear();
        foreach (var tray in _foundTrays)
        {
            var trayXform = Transform(tray);
            if (trayXform.GridUid != grid ||
                _container.IsEntityInContainer(tray) ||
                GetLivingPlant(tray.Comp) == null ||
                IsKnownActive(comp, tray) ||
                comp.SourceWhitelist != null && !_whitelist.IsValid(comp.SourceWhitelist, tray))
            {
                continue;
            }

            var trayMap = _transform.GetMapCoordinates(tray, trayXform);
            var distance = (trayMap.Position - hiveMap.Position).Length();
            if (distance <= comp.VisitRange && ClearFlightPath(hiveMap, trayMap, hive, tray))
                _candidates.Add((tray, distance));
        }

        _candidates.Sort((a, b) => a.Distance.CompareTo(b.Distance));
        for (var i = 0; i < _candidates.Count && found.Count < max; i++)
        {
            found.Add(_candidates[i].Tray);
        }
    }

    private static bool IsKnownActive(BeehiveComponent comp, EntityUid tray)
    {
        foreach (var known in comp.KnownSources)
        {
            if (known.Tray == tray && known.Active)
                return true;
        }

        return false;
    }

    /// <summary>
    /// The scout is back: the hive learns every tray it saw that still checks out.
    /// </summary>
    public void LandScout(Entity<BeeErrandComponent> scout)
    {
        var hiveUid = scout.Comp.Hive;
        if (TerminatingOrDeleted(hiveUid) || !TryComp<BeehiveComponent>(hiveUid, out var comp) || scout.Comp.Found is not { } found)
            return;

        Learn((hiveUid, comp), found);
        if (comp.ScoutFlyer == scout.Owner)
            comp.ScoutFlyer = null;
    }

    /// <summary>
    /// The hive learns the trays the scout found, those that still check out.
    /// </summary>
    private void Learn(Entity<BeehiveComponent> hive, List<EntityUid> found)
    {
        var comp = hive.Comp;
        var now = _timing.CurTime;
        var xform = Transform(hive);
        var hiveMap = _transform.GetMapCoordinates(hive, xform);

        foreach (var tray in found)
        {
            if (TerminatingOrDeleted(tray) || AddSource(hive, tray, now) is not { } source)
                continue;

            if (CheckSource(hive, xform, hiveMap, source, out _) == BeeSourceCheck.Inactive)
            {
                source.Active = false;
                source.NextVisit = now + comp.InactiveRecheck;
            }
        }
    }

    /// <summary>
    /// The scout in the air is gone at once: it was taken out, lost or the hive stopped. Its findings are lost too.
    /// </summary>
    public void RecallScout(BeehiveComponent comp)
    {
        if (comp.ScoutFlyer is { } flyer && !TerminatingOrDeleted(flyer))
            QueueDel(flyer);

        comp.ScoutFlyer = null;
    }

    /// <summary>
    /// True while this hive's scout is in the air.
    /// </summary>
    public bool ScoutFlying(BeehiveComponent comp)
    {
        if (comp.ScoutFlyer is not { } flyer)
            return false;

        if (!TerminatingOrDeleted(flyer))
            return true;

        comp.ScoutFlyer = null;
        return false;
    }

    /// <summary>
    /// The locker, closet or crate the entity sits in, if any. Bees fly in and out of a hive there even with the door
    /// shut.
    /// </summary>
    public EntityUid? StorageOf(EntityUid? uid)
    {
        if (uid is not { } inside ||
            !_container.TryGetContainingContainer(inside, out var container) ||
            !HasComp<EntityStorageComponent>(container.Owner))
        {
            return null;
        }

        return container.Owner;
    }

    /// <summary>
    /// Whether a bee can fly straight from one point to the other: no wall, window or closed door between them.
    /// Open doors let it through. The two ends themselves (the hive, the tray, the bee) never block.
    /// </summary>
    public bool ClearFlightPath(MapCoordinates from, MapCoordinates to, EntityUid? ignoreA = null, EntityUid? ignoreB = null)
    {
        if (from.MapId != to.MapId)
            return false;

        var delta = to.Position - from.Position;
        var length = delta.Length();
        if (length < 0.01f)
            return true;

        return FreeFlight(from, delta / length, length, ignoreA, ignoreB) >= length;
    }

    /// <summary>
    /// How far a bee flies from <paramref name="from"/> along <paramref name="direction"/> before something solid
    /// stops it, up to <paramref name="maxLength"/>.
    /// </summary>
    public float FreeFlight(MapCoordinates from, Vector2 direction, float maxLength, EntityUid? ignoreA = null,
        EntityUid? ignoreB = null)
    {
        // A hive or tray inside a locker: the locker does not block its own bees.
        var shellA = StorageOf(ignoreA);
        var shellB = StorageOf(ignoreB);

        // Not the first hit the query returns: that is not always the nearest one.
        var ray = new CollisionRay(from.Position, direction, (int) CollisionGroup.FlyingMobMask);
        var free = maxLength;
        var ignored = (ignoreA, ignoreB, shellA, shellB);
        foreach (var hit in _physics.IntersectRayWithPredicate(from.MapId, ray, ignored,
                     static (uid, ignored) => uid == ignored.ignoreA || uid == ignored.ignoreB ||
                                             uid == ignored.shellA || uid == ignored.shellB,
                     maxLength, returnOnFirstHit: false))
        {
            free = Math.Min(free, hit.Distance);
        }

        return free;
    }
}
