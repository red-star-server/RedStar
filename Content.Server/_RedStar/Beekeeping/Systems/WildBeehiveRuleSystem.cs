using System.Linq;
using System.Numerics;
using Content.Server._RedStar.Beekeeping.Components;
using Content.Server.Resist;
using Content.Server.StationEvents.Events;
using Content.Server.Storage.Components;
using Content.Server.Storage.EntitySystems;
using Content.Shared._RedStar.Beekeeping;
using Content.Shared._RedStar.Beekeeping.Components;
using Content.Shared.Access.Components;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.GameTicking.Components;
using Content.Shared.Lock;
using Content.Shared.Mobs.Components;
using Content.Shared.Storage.Components;
using Content.Shared.Tools.Systems;
using Robust.Shared.Configuration;
using Robust.Shared.Containers;
using Robust.Shared.Localization;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._RedStar.Beekeeping.Systems;

public sealed partial class WildBeehiveRuleSystem : StationEventSystem<WildBeehiveRuleComponent>
{
    [Dependency] private BeeSwarmSystem _swarms = default!;
    [Dependency] private BeehiveSystem _hives = default!;
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private EntityStorageSystem _storage = default!;
    [Dependency] private IConfigurationManager _config = default!;
    [Dependency] private ItemSlotsSystem _slots = default!;
    [Dependency] private SharedContainerSystem _containers = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private LockSystem _locks = default!;
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private WeldableSystem _weldable = default!;

    private static readonly EntProtoId WildHive = "WildBeehive";
    private static readonly EntProtoId Queen = "BeeQueen";

    protected override void Started(Entity<WildBeehiveRuleComponent, GameRuleComponent> ent, ref GameRuleStartedEvent args)
    {
        base.Started(ent, ref args);

        // The admin command bypasses the scheduler's MaxOccurrences.
        var previous = EntityQueryEnumerator<WildBeehiveRuleComponent>();
        while (previous.MoveNext(out _, out var rule))
        {
            if (rule.SpawnedHive)
                return;
        }

        var storages = Station.GetEntitiesWithComponentOnStation<EntityStorageComponent>(false, out var station);
        if (station is not { } target ||
            Station.GetLargestGrid(target.Owner) is not { } grid ||
            !TryComp<MapGridComponent>(grid, out var gridComp) ||
            Transform(grid).MapUid is not { } map ||
            _swarms.CountWildHives(map) >= _config.GetCVar(BeekeepingCVars.WildHiveLimit) ||
            !_hives.HasRoomForQueenOnMap(map))
        {
            return;
        }

        var lockers = new List<Entity<EntityStorageComponent>>();
        foreach (var storage in storages)
        {
            if (IsSuitableLocker(storage, ent.Comp1))
                lockers.Add(storage);
        }

        if (lockers.Count == 0)
            return;

        RobustRandom.Shuffle(lockers);
        var hive = SpawnAttachedTo(WildHive, Transform(lockers[0]).Coordinates);
        var size = _lookup.GetAABBNoContainer(hive, Vector2.Zero, 0).Size;
        foreach (var locker in lockers)
        {
            if (locker.Comp.MaxSize < MathF.Max(size.X, size.Y) || !_storage.Insert(hive, locker, locker.Comp))
                continue;

            var queen = SpawnAttachedTo(Queen, Transform(locker).Coordinates);
            if (!_slots.TryInsert(hive, Comp<BeehiveComponent>(hive).QueenSlot, queen, null))
            {
                Del(queen);
                break;
            }

            ent.Comp1.SpawnedHive = true;
            var offset = _transform.GetRelativePosition(Transform(locker), grid) - gridComp.LocalAABB.Center;
            var sector = GetSector(offset, gridComp.LocalAABB.Size);
            ChatSystem.DispatchStationAnnouncement(target.Owner,
                Loc.GetString("station-event-wild-beehive-announcement", ("sector", Loc.GetString(sector))));
            return;
        }

        Del(hive);
    }

    private bool IsSuitableLocker(Entity<EntityStorageComponent> locker, WildBeehiveRuleComponent rule)
    {
        if (MetaData(locker).EntityPrototype is not { } proto ||
            !MadeFrom(proto, rule.StorageParents))
        {
            return false;
        }

        var storage = locker.Comp;
        if (storage.Open || storage.ShowContents || storage.DeleteContentsOnDestruction ||
            storage.Contents.ContainedEntities.Count >= storage.Capacity ||
            !HasComp<ResistLockerComponent>(locker) || HasComp<AccessReaderComponent>(locker) ||
            HasComp<BluespaceLockerComponent>(locker) || _weldable.IsWelded(locker) || _locks.IsLocked(locker.Owner) ||
            _containers.IsEntityInContainer(locker))
        {
            return false;
        }

        foreach (var item in storage.Contents.ContainedEntities)
        {
            if (HasComp<BeehiveComponent>(item) || HasComp<MobStateComponent>(item))
                return false;
        }

        return true;
    }

    /// <summary>
    /// Whether the prototype is one of these or inherits one. Abstract bases are never indexed, so they are matched
    /// by name in the parents lists of the prototypes that are.
    /// </summary>
    private bool MadeFrom(EntityPrototype proto, List<EntProtoId> bases)
    {
        foreach (var known in _proto.EnumerateParents(proto, includeSelf: true))
        {
            if (bases.Contains(known.ID) || known.Parents?.Any(parent => bases.Contains(parent)) == true)
                return true;
        }

        return false;
    }

    private static LocId GetSector(Vector2 offset, Vector2 gridSize)
    {
        // Directions follow the station's map, regardless of the grid's rotation in space.
        var x = offset.X / MathF.Max(gridSize.X, 1f);
        var y = offset.Y / MathF.Max(gridSize.Y, 1f);
        if (MathF.Abs(x) <= 0.125f && MathF.Abs(y) <= 0.125f)
            return "station-event-wild-beehive-sector-central";

        if (MathF.Abs(x) > MathF.Abs(y))
            return x > 0f ? "station-event-wild-beehive-sector-east" : "station-event-wild-beehive-sector-west";

        return y > 0f ? "station-event-wild-beehive-sector-north" : "station-event-wild-beehive-sector-south";
    }
}
