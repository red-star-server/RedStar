using System.Numerics;
using Content.Server._RedStar.Beekeeping.Components;
using Content.Server._RedStar.Beekeeping.Events;
using Content.Server.Atmos.Piping.Unary.Components;
using Content.Server.StationEvents.Events;
using Content.Shared._RedStar.Beekeeping;
using Content.Shared._RedStar.Beekeeping.Components;
using Content.Shared._RedStar.Beekeeping.Data;
using Content.Shared.GameTicking.Components;
using Content.Shared.Station.Components;
using Content.Shared.Tools.Systems;
using Robust.Shared.Configuration;
using Robust.Shared.Containers;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;

namespace Content.Server._RedStar.Beekeeping.Systems;

/// <summary>
/// A wild swarm comes out of a station vent with a locker or crate and something to feed on near it, and settles there
/// by the usual swarm rules. The station hears about it once the hive is built.
/// </summary>
public sealed partial class WildBeehiveRuleSystem : StationEventSystem<WildBeehiveRuleComponent>
{
    [Dependency] private BeeSwarmSystem _swarms = default!;
    [Dependency] private BeehiveSystem _hives = default!;
    [Dependency] private MeatBeehiveSystem _meat = default!;
    [Dependency] private IConfigurationManager _config = default!;
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private SharedContainerSystem _containers = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private WeldableSystem _weldable = default!;

    private static readonly EntProtoId Swarm = "BeeSwarm";
    private static readonly EntProtoId WildHive = "WildBeehive";
    private static readonly EntProtoId Queen = "BeeQueen";

    private readonly List<EntityUid> _vents = new();

    protected override void Started(Entity<WildBeehiveRuleComponent, GameRuleComponent> ent, ref GameRuleStartedEvent args)
    {
        base.Started(ent, ref args);

        // The admin command bypasses the scheduler's MaxOccurrences.
        var previous = EntityQueryEnumerator<WildBeehiveRuleComponent>();
        while (previous.MoveNext(out _, out var rule))
        {
            if (rule.Released)
            {
                ForceEndSelf((ent.Owner, ent.Comp2));
                return;
            }
        }

        if (!Station.TryGetRandomStation<StationEventEligibleComponent>(out var station) ||
            Station.GetLargestGrid((station.Value.Owner, station.Value.Comp1)) is not { } grid ||
            Transform(grid).MapUid is not { } map ||
            _swarms.CountWildHives(map) >= _config.GetCVar(BeekeepingCVars.WildHiveLimit) ||
            !_hives.HasRoomForQueenOnMap(map) ||
            !_proto.Index(Queen).TryGetComponent<BeeQueenComponent>(out var queenData, Factory) ||
            !_proto.Index(WildHive).TryGetComponent<BeehiveComponent>(out var hiveData, Factory))
        {
            ForceEndSelf((ent.Owner, ent.Comp2));
            return;
        }

        _vents.Clear();
        var vents = EntityQueryEnumerator<GasVentPumpComponent, TransformComponent>();
        while (vents.MoveNext(out var vent, out _, out var xform))
        {
            if (xform.GridUid == grid && !_weldable.IsWelded(vent))
                _vents.Add(vent);
        }

        // Like any swarm, it lives on plants if there are any near a vent it can nest by; with none it hunts, out of
        // the quietest such vent. With nothing to feed on anywhere, no swarm comes: the hive would only sit there.
        var nests = _swarms.NestExits(_vents, queenData.NestRange, grid, queenData.NestParents);
        EntityUid exit;
        BeehiveDiet diet;
        if (_swarms.RichestExit(nests, hiveData.DietRange) is { } plantExit)
        {
            exit = plantExit;
            diet = BeehiveDiet.Plants;
        }
        else if (HasPrey(nests, hiveData) &&
                 _meat.CountMeatHives(map) < _config.GetCVar(BeekeepingCVars.MeatHiveLimit) &&
                 _swarms.QuietestExit(nests, BeeSwarmSystem.QuietRadius) is { } quietExit)
        {
            exit = quietExit;
            diet = BeehiveDiet.Meat;
        }
        else
        {
            ForceEndSelf((ent.Owner, ent.Comp2));
            return;
        }

        var coords = Transform(exit).Coordinates;
        var swarm = Spawn(Swarm, coords);
        var swarmComp = Comp<BeeSwarmComponent>(swarm);
        var queen = Spawn(Queen, coords);
        var slot = _containers.EnsureContainer<ContainerSlot>(swarm, swarmComp.Container);
        if (!_containers.Insert(queen, slot, force: true))
        {
            Del(queen);
            Del(swarm);
            ForceEndSelf((ent.Owner, ent.Comp2));
            return;
        }

        swarmComp.Bees = hiveData.StartingBees;
        swarmComp.Diet = diet;
        swarmComp.NestRange = queenData.NestRange;
        EnsureComp<WildBeehiveEventSwarmComponent>(swarm).Rule = ent.Owner;
        ent.Comp1.Released = true;
        _swarms.SwarmLeftVent((swarm, swarmComp));
    }

    private bool HasPrey(List<EntityUid> nests, BeehiveComponent hiveData)
    {
        foreach (var vent in nests)
        {
            if (_meat.CountDietCreatures(hiveData, _transform.GetMapCoordinates(vent), hiveData.DietRange) > 0)
                return true;
        }

        return false;
    }

    [SubscribeLocalEvent]
    private void OnSwarmLanded(Entity<WildBeehiveEventSwarmComponent> ent, ref BeeSwarmLandedEvent args)
    {
        var xform = Transform(args.Hive);
        if (Station.GetOwningStation(args.Hive, xform) is not { } station ||
            xform.GridUid is not { } grid ||
            !TryComp<MapGridComponent>(grid, out var gridComp))
        {
            return;
        }

        var offset = _transform.GetRelativePosition(xform, grid) - gridComp.LocalAABB.Center;
        var sector = GetSector(offset, gridComp.LocalAABB.Size);
        ChatSystem.DispatchStationAnnouncement(station,
            Loc.GetString("station-event-wild-beehive-announcement", ("sector", Loc.GetString(sector))));
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
