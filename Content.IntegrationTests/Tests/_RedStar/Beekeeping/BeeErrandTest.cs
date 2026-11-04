#nullable enable
using Content.IntegrationTests.Fixtures;
using Content.Server._RedStar.Beekeeping.Components;
using Content.Server._RedStar.Beekeeping.Systems;
using Content.Server.Atmos.EntitySystems;
using Content.Shared._RedStar.Beekeeping.Components;
using Content.Shared.Atmos;
using Content.Shared.Hands.EntitySystems;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;

namespace Content.IntegrationTests.Tests._RedStar.Beekeeping;

/// <summary>
/// A visible forager is an NPC on an errand: it flies to the tray by itself, visits it and comes home, and the colony
/// keeps its bee. A forager that is killed on the way is a bee the colony loses. Caught by hand it is the same bee,
/// counted outside its hive, and let go it flies back in.
/// </summary>
[TestOf(typeof(BeeErrandSystem))]
public sealed class BeeErrandTest : GameTest
{
    private EntityUid _hive;
    private EntityUid _tray;

    private async Task Build(TestMapData map)
    {
        var server = Server;
        await server.WaitPost(() =>
        {
            // Breathable air: bees do not fly into vacuum.
            var air = new GasMixture(Atmospherics.CellVolume)
            {
                Temperature = Atmospherics.T20C,
            };
            air.AdjustMoles(Gas.Oxygen, Atmospherics.OxygenMolesStandard);
            air.AdjustMoles(Gas.Nitrogen, Atmospherics.NitrogenMolesStandard);
            server.System<AtmosphereSystem>().SetMapAtmosphere(map.MapUid, false, air);

            var mapSystem = server.System<SharedMapSystem>();
            for (var x = 0; x <= 5; x++)
            {
                for (var y = -1; y <= 1; y++)
                    mapSystem.SetTile(map.Grid.Owner, map.Grid.Comp, new Vector2i(x, y), map.Tile.Tile);
            }

            _hive = server.EntMan.SpawnEntity("Beehive", new EntityCoordinates(map.Grid, 0.5f, 0.5f));
            _tray = server.EntMan.SpawnEntity("HydroponicsTrayEmpty", new EntityCoordinates(map.Grid, 5.5f, 0.5f));
            server.EntMan.GetComponent<BeehiveComponent>(_hive).Bees = 5;
        });
        await RunTicksSync(10);
    }

    [Test]
    public async Task ForagerFliesThereAndHome()
    {
        var map = await Pair.CreateTestMap();
        await Build(map);
        var server = Server;
        EntityUid bee = default;

        await server.WaitPost(() =>
        {
            var hive = new Entity<BeehiveComponent>(_hive, server.EntMan.GetComponent<BeehiveComponent>(_hive));
            bee = server.System<BeeErrandSystem>().TrySpawnForager(hive, server.EntMan.GetComponent<TransformComponent>(_hive), _tray)
                  ?? EntityUid.Invalid;
            Assert.That(bee.IsValid(), "the forager flew out");
        });

        // It gets near the tray by itself.
        var reached = false;
        for (var i = 0; i < 60 && !reached; i++)
        {
            await RunTicksSync(15);
            await server.WaitPost(() =>
            {
                if (server.EntMan.Deleted(bee))
                    return;

                var xforms = server.System<SharedTransformSystem>();
                reached = (xforms.GetWorldPosition(bee) - xforms.GetWorldPosition(_tray)).Length() < 1.5f;
            });
        }

        Assert.That(reached, "the forager reached the tray");

        await RunSeconds(20);
        await server.WaitAssertion(() =>
        {
            Assert.That(server.EntMan.Deleted(bee), "the forager came home");
            Assert.That(server.EntMan.GetComponent<BeehiveComponent>(_hive).Bees, Is.EqualTo(5), "the colony kept its bee");
        });
    }

    [Test]
    public async Task KilledForagerIsLost()
    {
        var map = await Pair.CreateTestMap();
        await Build(map);
        var server = Server;

        await server.WaitAssertion(() =>
        {
            var hive = new Entity<BeehiveComponent>(_hive, server.EntMan.GetComponent<BeehiveComponent>(_hive));
            var bee = server.System<BeeErrandSystem>().TrySpawnForager(hive, server.EntMan.GetComponent<TransformComponent>(_hive), _tray);
            Assert.That(bee, Is.Not.Null);
            server.EntMan.DeleteEntity(bee!.Value);
            Assert.That(hive.Comp.Bees, Is.EqualTo(4), "the colony lost the bee");
        });
    }

    [Test]
    public async Task CaughtForagerStaysTheSameBee()
    {
        var map = await Pair.CreateTestMap();
        await Build(map);
        var server = Server;
        var hands = server.System<SharedHandsSystem>();
        EntityUid bee = default;
        EntityUid user = default;

        await server.WaitAssertion(() =>
        {
            var hive = new Entity<BeehiveComponent>(_hive, server.EntMan.GetComponent<BeehiveComponent>(_hive));
            bee = server.System<BeeErrandSystem>().TrySpawnForager(hive, server.EntMan.GetComponent<TransformComponent>(_hive), _tray)
                  ?? EntityUid.Invalid;
            Assert.That(bee.IsValid(), "the forager flew out");
            Assert.That(server.EntMan.GetComponent<BeeItemComponent>(bee).SourceHive, Is.EqualTo(_hive),
                "a bee in the air already knows its hive, so the analyzer can read it");

            user = server.EntMan.SpawnEntity("MobHuman", new EntityCoordinates(map.Grid, 1.5f, 0.5f));
            Assert.That(hands.TryPickupAnyHand(user, bee, checkActionBlocker: false), "the forager was caught");

            var item = server.EntMan.GetComponent<BeeItemComponent>(bee);
            Assert.Multiple(() =>
            {
                Assert.That(server.EntMan.HasComponent<BeeErrandComponent>(bee), Is.False,
                    "the caught bee left its errand");
                Assert.That(item.CountedOutside, "it is counted outside its hive");
                Assert.That(item.SourceHive, Is.EqualTo(_hive));
                Assert.That(hive.Comp.Bees, Is.EqualTo(4));
                Assert.That(hive.Comp.OutBees, Is.EqualTo(1));
            });

            Assert.That(hands.TryDrop(user, bee, checkActionBlocker: false), "the bee was let go");
        });

        await RunSeconds(20);
        await server.WaitAssertion(() =>
        {
            var hive = server.EntMan.GetComponent<BeehiveComponent>(_hive);
            Assert.Multiple(() =>
            {
                Assert.That(server.EntMan.Deleted(bee), "the bee flew back into its hive");
                Assert.That(hive.Bees, Is.EqualTo(5));
                Assert.That(hive.OutBees, Is.Zero);
            });
        });
    }
}
