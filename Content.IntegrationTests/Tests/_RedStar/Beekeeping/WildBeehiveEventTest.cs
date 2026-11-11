#nullable enable
using Content.Shared.Tools.Components;
using Content.Shared._RedStar.Beekeeping.Data;
using Content.Shared.Botany.Systems;
using System.Numerics;
using System.Collections.Generic;
using System.Linq;
using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.Server._RedStar.Beekeeping.Components;
using Content.Server._RedStar.Beekeeping.Systems;
using Content.Server.Atmos.EntitySystems;
using Content.Server.GameTicking;
using Content.Server.Station.Systems;
using Content.Server.StationEvents;
using Content.Server.Storage.EntitySystems;
using Content.Shared._RedStar.Beekeeping;
using Content.Shared._RedStar.Beekeeping.Components;
using Content.Shared.Atmos;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.EntityTable.EntitySelectors;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Station.Components;
using Content.Shared.Storage.Components;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Robust.UnitTesting.Pool;

namespace Content.IntegrationTests.Tests._RedStar.Beekeeping;

public sealed class WildBeehiveEventTest : GameTest
{
    private static readonly EntProtoId Rule = "WildBeehiveEvent";
    private readonly List<EntityUid> _rules = new();
    private static PoolSettings StationScenario => new() { Connected = true, Fresh = true };

    [TearDown]
    public async Task RemoveRules()
    {
        await Server.WaitPost(() =>
        {
            foreach (var rule in _rules)
                Server.EntMan.DeleteEntity(rule);
        });
    }

    [Test]
    [PairConfig(nameof(StationScenario))]
    public async Task SwarmFromAVentSettlesNearPlantsAndItsQueenCanStartAnApiary()
    {
        var server = Server;
        var map = await Pair.CreateTestMap();
        var entities = server.EntMan;
        var hives = server.System<BeehiveSystem>();
        var swarms = server.System<BeeSwarmSystem>();
        var containers = server.System<SharedContainerSystem>();
        EntityUid queen = default;
        EntityUid queenFlyer = default;

        await server.WaitAssertion(() =>
        {
            BuildRoom(map);
            var grid = map.Grid.Owner;
            // A bare vent with a locker but nothing to eat, and far from it one with a locker and a planted tray.
            entities.SpawnEntity("GasVentPump", At(grid, -12, 0));
            entities.SpawnEntity("ClosetMaintenance", At(grid, -11, 0));
            var vent = entities.SpawnEntity("GasVentPump", At(grid, 4, 0));
            var locker = entities.SpawnEntity("ClosetMaintenance", At(grid, 6, 0));
            Plant(At(grid, 3, 1));
            var visitor = entities.SpawnEntity("MobHuman", At(grid, 0, 0));

            StartEvent();
            var query = entities.EntityQueryEnumerator<WildBeehiveEventSwarmComponent, BeeSwarmComponent>();
            Assert.That(query.MoveNext(out var swarm, out _, out var swarmComp), Is.True, "a swarm came out");
            var swarmPos = entities.GetComponent<TransformComponent>(swarm).LocalPosition;
            var ventPos = entities.GetComponent<TransformComponent>(vent).LocalPosition;
            Assert.Multiple(() =>
            {
                Assert.That((swarmPos - ventPos).Length(), Is.LessThan(0.1f), "out of the vent with plants near it");
                Assert.That(swarmComp!.Diet, Is.EqualTo(BeehiveDiet.Plants));
                Assert.That(swarmComp.Storage, Is.EqualTo(locker));
                Assert.That(swarmComp.Bees, Is.GreaterThan(0));
            });

            // Its flight is the one of any swarm; here it lands at once.
            swarms.LandSwarm((swarm, swarmComp!));
            // The errand ends the swarm once it has landed.
            entities.DeleteEntity(swarm);
            var storage = entities.GetComponent<EntityStorageComponent>(locker);
            var hive = storage.Contents.ContainedEntities.Single(uid => entities.HasComponent<BeehiveComponent>(uid));
            var colony = entities.GetComponent<BeehiveComponent>(hive);
            var resident = hives.GetQueen((hive, colony));
            Assert.That(resident, Is.Not.Null);
            queen = resident!.Value;
            Assert.Multiple(() =>
            {
                Assert.That(colony.Wild, Is.True);
                Assert.That(colony.Diet, Is.EqualTo(BeehiveDiet.Plants));
                Assert.That(colony.Bees, Is.GreaterThan(0));
                Assert.That(entities.EntityQuery<BeehiveAngryBeeComponent>(), Is.Empty);
            });

            server.System<EntityStorageSystem>().OpenStorage(locker);
            var damage = server.System<DamageableSystem>();
            Assert.That(damage.TryChangeDamage(hive, new DamageSpecifier { DamageDict = { ["Blunt"] = 25 } }, ignoreResistances: true, origin: visitor));
            Assert.That(entities.EntityQuery<BeehiveAngryBeeComponent>(), Is.Not.Empty);
            damage.TryChangeDamage(hive, new DamageSpecifier { DamageDict = { ["Blunt"] = 200 } }, origin: visitor);

            var flyer = entities.EntityQueryEnumerator<BeehiveAngryQueenComponent>();
            Assert.That(flyer.MoveNext(out queenFlyer, out _), Is.True);
            Assert.That(containers.TryGetContainingContainer(queen, out var container), Is.True);
            Assert.That(container!.Owner, Is.EqualTo(queenFlyer));
        });
        await RunTicksSync(5);

        await server.WaitAssertion(() =>
        {
            Assert.That(entities.EntityExists(queen), Is.True);
            var angry = entities.GetComponent<BeehiveAngryQueenComponent>(queenFlyer);
            // Advance the defense timeout once the destroyed hive is gone.
            angry.AngrySince -= angry.MaxAngry;
            angry.NextCheck = server.Timing.CurTime;
        });
        await RunSeconds(1);

        await server.WaitAssertion(() =>
        {
            Assert.That(entities.Deleted(queenFlyer), Is.True);
            Assert.That(entities.EntityExists(queen), Is.True);
            var hands = server.System<SharedHandsSystem>();
            var beekeeper = entities.SpawnAttachedTo("MobHuman", map.GridCoords);
            Assert.That(containers.IsEntityInContainer(queen), Is.False, "the queen stays available after landing");
            Assert.That(hands.TryPickup(beekeeper, queen, animate: false), Is.True);

            var managed = entities.SpawnAttachedTo("Beehive", map.GridCoords);
            var managedColony = entities.GetComponent<BeehiveComponent>(managed);
            var slots = server.System<ItemSlotsSystem>();
            Assert.That(slots.TryInsert(managed, managedColony.QueenSlot, queen, null), Is.True);
            Assert.That(hives.GetQueen((managed, managedColony)), Is.EqualTo(queen));

            // A second manual start releases no other swarm.
            StartEvent();
            Assert.That(entities.EntityQuery<WildBeehiveEventSwarmComponent>(), Is.Empty);
        });
    }

    [TestCase("nothing-to-eat")]
    [TestCase("meat-limit")]
    [TestCase("not-a-locker")]
    [TestCase("welded")]
    [TestCase("wild-limit")]
    [TestCase("active-limit")]
    public async Task EventSkipsAStationThatCannotHostAHive(string reason)
    {
        var server = Server;
        var map = await Pair.CreateTestMap();
        var entities = server.EntMan;
        if (reason == "wild-limit")
            await OverrideCVar(Side.Server, BeekeepingCVars.WildHiveLimit, 0);
        if (reason == "active-limit")
            await OverrideCVar(Side.Server, BeekeepingCVars.ActiveHiveLimit, 0);
        if (reason == "meat-limit")
            await OverrideCVar(Side.Server, BeekeepingCVars.MeatHiveLimit, 0);

        await server.WaitAssertion(() =>
        {
            BuildRoom(map);
            var grid = map.Grid.Owner;
            var vent = entities.SpawnEntity("GasVentPump", At(grid, 0, 0));
            // A pet carrier also lets its occupant break out, but it is no locker or crate.
            entities.SpawnEntity(reason == "not-a-locker" ? "PetCarrier" : "ClosetMaintenance", At(grid, 2, 0));
            // With no plants a swarm would hunt: the meat limit leaves it nothing to live on.
            if (reason == "meat-limit")
                entities.SpawnEntity("MobHuman", At(grid, -2, 0));
            else if (reason != "nothing-to-eat")
                Plant(At(grid, -1, 1));
            if (reason == "welded")
                entities.GetComponent<WeldableComponent>(vent).IsWelded = true;

            StartEvent();
            Assert.That(entities.EntityQuery<BeeSwarmComponent>(), Is.Empty);
            Assert.That(entities.EntityQuery<BeeQueenComponent>(), Is.Empty);
        });
    }

    [Test]
    public async Task WithNoPlantsTheSwarmHuntsFromTheQuietestVent()
    {
        var server = Server;
        var map = await Pair.CreateTestMap();
        var entities = server.EntMan;
        await server.WaitAssertion(() =>
        {
            BuildRoom(map);
            var grid = map.Grid.Owner;
            // Two vents with a locker each, nothing planted; a crewman stands by one of them.
            entities.SpawnEntity("GasVentPump", At(grid, -12, 0));
            entities.SpawnEntity("ClosetMaintenance", At(grid, -11, 0));
            entities.SpawnEntity("MobHuman", At(grid, -13, 0));
            var quiet = entities.SpawnEntity("GasVentPump", At(grid, 4, 0));
            entities.SpawnEntity("ClosetMaintenance", At(grid, 6, 0));

            StartEvent();
            var query = entities.EntityQueryEnumerator<WildBeehiveEventSwarmComponent, BeeSwarmComponent>();
            Assert.That(query.MoveNext(out var swarm, out _, out var swarmComp), Is.True, "a swarm came out");
            var swarmPos = entities.GetComponent<TransformComponent>(swarm).LocalPosition;
            var ventPos = entities.GetComponent<TransformComponent>(quiet).LocalPosition;
            Assert.Multiple(() =>
            {
                Assert.That(swarmComp!.Diet, Is.EqualTo(BeehiveDiet.Meat));
                Assert.That((swarmPos - ventPos).Length(), Is.LessThan(0.1f), "out of the vent with nobody near it");
            });
        });
    }

    [TestCase("BasicGameRulesTable")]
    [TestCase("DynamicGameRulesTable")]
    public async Task SchedulerAdmitsTheEventFromMinuteTwentyOnlyOnce(string tableId)
    {
        var server = Server;
        var map = await Pair.CreateTestMap();
        var events = server.System<EventManagerSystem>();
        var table = new NestedSelector { TableId = tableId };
        await server.WaitAssertion(() =>
        {
            BuildRoom(map);
            var grid = map.Grid.Owner;
            server.EntMan.SpawnEntity("GasVentPump", At(grid, 0, 0));
            server.EntMan.SpawnEntity("ClosetMaintenance", At(grid, 2, 0));
            Assert.That(IsAvailableAt(20 - 1d / 60), Is.False);
            Assert.That(IsAvailableAt(20), Is.True);

            // Nothing to feed on: the event ends itself and does not use up its one occurrence.
            StartEvent();
        });
        await RunSeconds(2);
        await server.WaitAssertion(() =>
        {
            Assert.That(IsAvailableAt(90), Is.True);
            Plant(At(map.Grid.Owner, -1, 1));
            StartEvent();
        });
        await RunSeconds(2);
        await server.WaitAssertion(() => Assert.That(IsAvailableAt(90), Is.False));

        bool IsAvailableAt(double minute)
        {
            return events.TryBuildLimitedEvents(table, out var eligible, TimeSpan.FromMinutes(minute), 25) &&
                eligible.Value.Events.Any(entry => entry.Item1.ID == Rule);
        }
    }

    private static EntityCoordinates At(EntityUid grid, float x, float y)
    {
        return new EntityCoordinates(grid, new Vector2(x + 0.5f, y + 0.5f));
    }

    /// <summary>
    /// A breathable room of plating around the origin, with a station on its grid.
    /// </summary>
    private void BuildRoom(TestMapData map)
    {
        var maps = Server.System<SharedMapSystem>();
        for (var x = -14; x <= 8; x++)
        {
            for (var y = -2; y <= 2; y++)
                maps.SetTile(map.Grid.Owner, map.Grid.Comp, new Vector2i(x, y), map.Tile.Tile);
        }

        var air = new GasMixture(Atmospherics.CellVolume) { Temperature = Atmospherics.T20C };
        air.SetMoles(Gas.Oxygen, 21.8f);
        air.SetMoles(Gas.Nitrogen, 82.1f);
        Server.System<AtmosphereSystem>().SetMapAtmosphere(map.MapUid, false, air);
        AddStation(map.Grid.Owner);
    }

    /// <summary>
    /// A tray with a living wheat plant in it.
    /// </summary>
    private void Plant(EntityCoordinates coords)
    {
        var entities = Server.EntMan;
        var tray = entities.SpawnEntity("HydroponicsTrayEmpty", coords);
        var plant = entities.SpawnEntity("WheatPlants", coords);
        Server.System<PlantTraySystem>().PlantingPlantInTray(tray, plant);
    }

    private void StartEvent()
    {
        var ticker = Server.System<ServerGameTicker>();
        Assert.That(ticker.StartGameRule(Rule, out var rule), Is.True);
        _rules.Add(rule!.Value.Owner);
    }

    private void AddStation(EntityUid grid)
    {
        var entities = Server.EntMan;
        var station = SSpawn(null);
        entities.AddComponent<StationDataComponent>(station);
        entities.AddComponent<StationEventEligibleComponent>(station);
        Server.System<ServerStationSystem>().AddGridToStation(station, grid);
    }
}
