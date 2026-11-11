#nullable enable
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
    public async Task EventHiveDefendsItselfAndItsQueenCanStartAnApiary()
    {
        var server = Server;
        var map = await Pair.CreateTestMap();
        var entities = server.EntMan;
        var hives = server.System<BeehiveSystem>();
        var containers = server.System<SharedContainerSystem>();
        EntityUid queen = default;
        EntityUid queenFlyer = default;

        await server.WaitAssertion(() =>
        {
            var maps = server.System<SharedMapSystem>();
            for (var x = -2; x <= 2; x++)
            {
                for (var y = -2; y <= 2; y++)
                    maps.SetTile(map.Grid.Owner, map.Grid.Comp, new Vector2i(x, y), map.Tile.Tile);
            }

            var air = new GasMixture(Atmospherics.CellVolume) { Temperature = Atmospherics.T20C };
            air.SetMoles(Gas.Oxygen, 21.8f);
            air.SetMoles(Gas.Nitrogen, 82.1f);
            server.System<AtmosphereSystem>().SetMapAtmosphere(map.MapUid, false, air);

            AddStation(map.Grid.Owner);
            var locker = entities.SpawnAttachedTo("ClosetMaintenance", map.GridCoords);
            var storage = entities.GetComponent<EntityStorageComponent>(locker);
            var visitor = entities.SpawnAttachedTo("MobHuman", map.GridCoords);

            StartEvent();
            var hive = storage.Contents.ContainedEntities.Single(uid => entities.HasComponent<BeehiveComponent>(uid));
            var colony = entities.GetComponent<BeehiveComponent>(hive);
            var resident = hives.GetQueen((hive, colony));
            Assert.That(resident, Is.Not.Null);
            queen = resident!.Value;
            Assert.Multiple(() =>
            {
                Assert.That(colony.Wild, Is.True);
                Assert.That(colony.Bees, Is.EqualTo(colony.StartingBees).And.GreaterThan(0));
                Assert.That(entities.EntityQuery<BeehiveAngryBeeComponent>(), Is.Empty);
            });

            server.System<EntityStorageSystem>().OpenStorage(locker);
            Assert.That(entities.EntityQuery<BeehiveAngryBeeComponent>(), Is.Empty);

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
            Assert.That(hands.GetActiveItem(beekeeper), Is.EqualTo(queen));

            var managed = entities.SpawnAttachedTo("Beehive", map.GridCoords);
            var managedColony = entities.GetComponent<BeehiveComponent>(managed);
            var slots = server.System<ItemSlotsSystem>();
            Assert.That(slots.TryInsert(managed, managedColony.QueenSlot, queen, null), Is.True);
            Assert.That(hives.GetQueen((managed, managedColony)), Is.EqualTo(queen));
            Assert.That(managedColony.Bees, Is.GreaterThan(0));

            // A second manual start must not create another hive even when the original is gone.
            var otherLocker = entities.SpawnAttachedTo("ClosetMaintenance", map.GridCoords);
            StartEvent();
            Assert.That(entities.GetComponent<EntityStorageComponent>(otherLocker).Contents.ContainedEntities, Is.Empty);
        });
    }

    [TestCase("open")]
    [TestCase("wild-limit")]
    [TestCase("active-limit")]
    [TestCase("not-a-locker")]
    public async Task EventSkipsAStationThatCannotHostAHive(string reason)
    {
        var server = Server;
        var map = await Pair.CreateTestMap();
        var entities = server.EntMan;
        if (reason == "wild-limit")
            await OverrideCVar(Side.Server, BeekeepingCVars.WildHiveLimit, 0);
        if (reason == "active-limit")
            await OverrideCVar(Side.Server, BeekeepingCVars.ActiveHiveLimit, 0);

        await server.WaitAssertion(() =>
        {
            AddStation(map.Grid.Owner);
            // A pet carrier also lets its occupant break out, but it is no locker or crate.
            var locker = entities.SpawnAttachedTo(reason == "not-a-locker" ? "PetCarrier" : "ClosetMaintenance", map.GridCoords);
            if (reason == "open")
                server.System<EntityStorageSystem>().OpenStorage(locker);

            StartEvent();
            Assert.That(server.System<BeeSwarmSystem>().CountWildHives(map.MapUid), Is.Zero);
            Assert.That(entities.EntityQuery<BeeQueenComponent>(), Is.Empty);
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
            AddStation(map.Grid.Owner);
            server.EntMan.SpawnAttachedTo("ClosetMaintenance", map.GridCoords);
            Assert.That(IsAvailableAt(20 - 1d / 60), Is.False);
            Assert.That(IsAvailableAt(20), Is.True);
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
