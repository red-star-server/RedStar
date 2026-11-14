#nullable enable
using System.Numerics;
using System.Linq;
using Content.IntegrationTests.Fixtures;
using Content.Server._RedStar.Beekeeping.Components;
using Content.Server._RedStar.Beekeeping.Data;
using Content.Server._RedStar.Beekeeping.Systems;
using Content.Shared.Destructible;
using Content.Shared.Tools.Components;
using Content.Shared._RedStar.Beekeeping;
using Content.Shared._RedStar.Beekeeping.Components;
using Content.Shared._RedStar.Beekeeping.Data;
using Content.Shared._RedStar.Beekeeping.Visuals;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;

namespace Content.IntegrationTests.Tests._RedStar.Beekeeping;

/// <summary>
/// The foragers of a meat colony go through the vents when the target is out of sight, and each one killed makes the
/// colony furious.
/// </summary>
[TestOf(typeof(BeehiveSystem))]
public sealed class MeatHuntTest : GameTest
{
    private EntityUid _hive;
    private EntityUid _entry;
    private EntityUid _exit;
    private EntityUid _human;

    private async Task Build(TestMapData map)
    {
        var server = Server;
        await server.WaitPost(() =>
        {
            var grid = map.Grid.Owner;
            var mapSys = server.System<SharedMapSystem>();
            var plating = new Tile(server.ResolveDependency<ITileDefinitionManager>()["Plating"].TileId);
            for (var x = 0; x <= 9; x++)
            {
                for (var y = 0; y <= 2; y++)
                    mapSys.SetTile(grid, map.Grid.Comp, new Vector2i(x, y), plating);
            }

            // Breathable air: bees do not dive into vacuum.
            var air = new Content.Shared.Atmos.GasMixture(Content.Shared.Atmos.Atmospherics.CellVolume)
            {
                Temperature = Content.Shared.Atmos.Atmospherics.T20C,
            };
            air.AdjustMoles(Content.Shared.Atmos.Gas.Oxygen, Content.Shared.Atmos.Atmospherics.OxygenMolesStandard);
            air.AdjustMoles(Content.Shared.Atmos.Gas.Nitrogen, Content.Shared.Atmos.Atmospherics.NitrogenMolesStandard);
            server.System<Content.Server.Atmos.EntitySystems.AtmosphereSystem>().SetMapAtmosphere(map.MapUid, false, air);

            EntityCoordinates At(float x, float y) => new(grid, new Vector2(x + 0.5f, y + 0.5f));

            _hive = server.EntMan.SpawnEntity("Beehive", At(0, 0));
            server.EntMan.GetComponent<BeehiveComponent>(_hive).Diet = BeehiveDiet.Meat;

            // Two vents on one pipe, with a wall between the hive and the man by the far vent.
            _entry = server.EntMan.SpawnEntity("GasVentPump", At(2, 1));
            _exit = server.EntMan.SpawnEntity("GasVentPump", At(7, 1));
            for (var x = 2; x <= 7; x++)
                server.EntMan.SpawnEntity("GasPipeFourway", At(x, 0));

            server.EntMan.SpawnEntity("WallSolid", At(5, 1));
            server.EntMan.SpawnEntity("WallSolid", At(5, 2));
            _human = server.EntMan.SpawnEntity("MobHuman", At(8, 2));
        });
        await RunTicksSync(20);
    }

    [Test]
    public async Task RouteThroughTheVents()
    {
        var map = await Pair.CreateTestMap();
        await Build(map);
        var server = Server;
        var hives = server.System<BeehiveSystem>();

        await server.WaitAssertion(() =>
        {
            var comp = server.EntMan.GetComponent<BeehiveComponent>(_hive);
            var origin = server.System<SharedTransformSystem>().GetMapCoordinates(_hive);
            var route = server.System<MeatBeehiveSystem>().FindHuntRoute((_hive, comp), origin, _human);
            Assert.That(route, Is.Not.Null);
            Assert.That(route!.Value.Entry, Is.EqualTo(_entry));
            Assert.That(route.Value.Exit, Is.EqualTo(_exit));

            // The man is a source because of the vents; weld the far one shut and he is not.
            var source = new BeehiveSource(_human, server.Timing.CurTime);
            Assert.That(server.System<BeeScoutSystem>().GetTrayState((_hive, comp), source), Is.EqualTo(ApiaryTrayState.Ripe));
            server.EntMan.GetComponent<WeldableComponent>(_exit).IsWelded = true;
            Assert.That(server.System<BeeScoutSystem>().GetTrayState((_hive, comp), source), Is.EqualTo(ApiaryTrayState.Blind));
        });
    }

    [Test]
    public async Task ForagerGoesThroughAndBites()
    {
        var map = await Pair.CreateTestMap();
        await Build(map);
        var server = Server;
        var hives = server.System<BeehiveSystem>();

        await server.WaitPost(() =>
        {
            var comp = server.EntMan.GetComponent<BeehiveComponent>(_hive);
            comp.Bees = 10;
            comp.VisitBudget = 1f;
            server.System<MeatBeehiveSystem>().Hunt((_hive, comp));
            Assert.That(comp.Foragers, Has.Count.EqualTo(1), "a forager flew off");
        });

        // Out of the hive, down the pipes, to the man and back: well within a minute.
        await RunTicksSync(30 * 40);

        await server.WaitAssertion(() =>
        {
            var comp = server.EntMan.GetComponent<BeehiveComponent>(_hive);
            Assert.That(server.System<MeatBeehiveSystem>().MeatOf(_hive).BloodTypes, Does.Contain((Robust.Shared.Prototypes.ProtoId<Content.Shared.Chemistry.Reagent.ReagentPrototype>) "Blood"),
                "the bite took blood");
        });
    }

    [Test]
    public async Task KillingAForagerEnragesTheColony()
    {
        var map = await Pair.CreateTestMap();
        await Build(map);
        var server = Server;
        var hives = server.System<BeehiveSystem>();

        await server.WaitPost(() =>
        {
            var comp = server.EntMan.GetComponent<BeehiveComponent>(_hive);
            comp.MaxBees = 20;
            comp.Bees = 10;
            Assert.That(server.System<MeatBeehiveSystem>().RageLevel((_hive, comp)), Is.Zero);

            comp.VisitBudget = 1f;
            server.System<MeatBeehiveSystem>().Hunt((_hive, comp));
            var bee = comp.Foragers[0];
            Assert.That(server.EntMan.GetComponent<MetaDataComponent>(bee).EntityPrototype!.ID, Is.EqualTo("BeeForagerMeat"));
            server.System<SharedDestructibleSystem>().DestroyEntity(bee);
        });
        await RunTicksSync(5);

        await server.WaitAssertion(() =>
        {
            var comp = server.EntMan.GetComponent<BeehiveComponent>(_hive);
            Assert.That(comp.Bees, Is.EqualTo(9), "the colony lost a bee");
            Assert.That(server.System<MeatBeehiveSystem>().RageLevel((_hive, comp)), Is.GreaterThanOrEqualTo(server.System<MeatBeehiveSystem>().MeatOf(_hive).RageMin));

            // The fewer bees left, the angrier.
            var before = server.System<MeatBeehiveSystem>().RageLevel((_hive, comp));
            comp.Bees = 2;
            Assert.That(server.System<MeatBeehiveSystem>().RageLevel((_hive, comp)), Is.GreaterThan(before));
        });
    }

    /// <summary>
    /// A vent welded shut while the bee is in the pipes: the bee is lost, the route and its slot are cleared, and the
    /// hive can send the next forager.
    /// </summary>
    [Test]
    public async Task WeldingTheExitLosesTheBeeCleanly()
    {
        var map = await Pair.CreateTestMap();
        await Build(map);
        var server = Server;
        var hives = server.System<BeehiveSystem>();
        EntityUid bee = default;

        await server.WaitPost(() =>
        {
            var comp = server.EntMan.GetComponent<BeehiveComponent>(_hive);
            comp.Bees = 10;
            comp.VisitBudget = 1f;
            server.System<MeatBeehiveSystem>().Hunt((_hive, comp));
            bee = comp.Foragers[0];
        });

        // Wait until it is in the pipes.
        var inPipe = false;
        for (var i = 0; i < 600 && !inPipe; i++)
        {
            await RunTicksSync(5);
            await server.WaitPost(() =>
            {
                inPipe = server.EntMan.TryGetComponent<BeeErrandComponent>(bee, out var errand) &&
                         errand.Act == BeeErrandAct.Pipe && !errand.PipedOut;
            });
        }

        Assert.That(inPipe, "the bee went into the pipes");
        await server.WaitPost(() => server.EntMan.GetComponent<WeldableComponent>(_exit).IsWelded = true);
        await RunTicksSync(30 * 10);

        await server.WaitAssertion(() =>
        {
            var comp = server.EntMan.GetComponent<BeehiveComponent>(_hive);
            Assert.That(server.EntMan.Deleted(bee) || server.EntMan.IsQueuedForDeletion(bee), "the bee is gone");
            Assert.That(comp.Foragers.Count(f => !server.EntMan.Deleted(f)), Is.Zero, "no slot is held");
            Assert.That(server.System<MeatBeehiveSystem>().MeatOf(_hive).BloodTypes, Is.Empty, "nothing was bitten");
        });
    }

    /// <summary>
    /// The target is gone before the bite: the bee cancels the attack, goes back and nothing is taken.
    /// </summary>
    [Test]
    public async Task TargetGoneBeforeTheBite()
    {
        var map = await Pair.CreateTestMap();
        await Build(map);
        var server = Server;
        var hives = server.System<BeehiveSystem>();

        await server.WaitPost(() =>
        {
            var comp = server.EntMan.GetComponent<BeehiveComponent>(_hive);
            comp.Bees = 10;
            comp.VisitBudget = 1f;
            server.System<MeatBeehiveSystem>().Hunt((_hive, comp));
            Assert.That(comp.Foragers, Has.Count.EqualTo(1));
            server.EntMan.DeleteEntity(_human);
        });

        await RunTicksSync(30 * 40);

        await server.WaitAssertion(() =>
        {
            var comp = server.EntMan.GetComponent<BeehiveComponent>(_hive);
            Assert.That(server.System<MeatBeehiveSystem>().MeatOf(_hive).BloodTypes, Is.Empty, "no bite");
            Assert.That(comp.Foragers.Count(f => !server.EntMan.Deleted(f)), Is.Zero, "the bee came back");
        });
    }

    /// <summary>
    /// A meat swarm flies to the place it will settle in: the quiet closet, not the nearer one with people beside it.
    /// </summary>
    [Test]
    public async Task MeatSwarmFliesToItsNest()
    {
        var map = await Pair.CreateTestMap();
        await Build(map);
        var server = Server;
        var hives = server.System<BeehiveSystem>();
        EntityUid near = default;
        EntityUid far = default;

        await server.WaitPost(() =>
        {
            var grid = map.Grid.Owner;
            // Remove what Build set up that is not needed here: the vents and the wall do not matter, closets do.
            near = server.EntMan.SpawnEntity("ClosetMaintenance", new EntityCoordinates(grid, new Vector2(2.5f, 1.5f)));
            far = server.EntMan.SpawnEntity("ClosetMaintenance", new EntityCoordinates(grid, new Vector2(9.5f, 0.5f)));
            var queen = server.EntMan.SpawnEntity("BeeQueen", new EntityCoordinates(grid, new Vector2(0.5f, 0.5f)));
            var comp = server.EntMan.GetComponent<BeehiveComponent>(_hive);
            Assert.That(server.System<Content.Shared.Containers.ItemSlots.ItemSlotsSystem>().TryInsert(_hive, comp.QueenSlot, queen, null));
            comp.Bees = 10;
            comp.MaxBees = 20;
            // Someone stands by the near closet.
            server.EntMan.SpawnEntity("MobHuman", new EntityCoordinates(grid, new Vector2(2.5f, 2.5f)));
        });
        await RunTicksSync(10);

        await server.WaitAssertion(() =>
        {
            // The vents of Build would take the swarm through the pipes; weld them so it flies to a closet.
            server.EntMan.GetComponent<WeldableComponent>(_entry).IsWelded = true;
            var comp = server.EntMan.GetComponent<BeehiveComponent>(_hive);
            Assert.That(server.System<BeeSwarmSystem>().TrySwarm((_hive, comp)), "the swarm left");

            var swarms = server.EntMan.AllEntityQueryEnumerator<BeeSwarmComponent, BeeErrandComponent>();
            Assert.That(swarms.MoveNext(out _, out var swarm, out var errand));
            Assert.That(swarm, Is.Not.Null);
            Assert.That(errand, Is.Not.Null);
            if (swarm!.Diet != BeehiveDiet.Meat)
                Assert.Ignore("the draw gave a plant swarm, nothing to check");

            Assert.That(swarm.Storage, Is.Not.Null);
            Assert.That(errand!.Target, Is.EqualTo(swarm.Storage), "it flies to the closet it will settle in");
        });
    }

    /// <summary>
    /// The analyzer shows a creature the hive bit as its kind only: the species of a humanoid, a plain picture, and no
    /// name or entity that would identify the person.
    /// </summary>
    [Test]
    public async Task AnalyzerShowsOnlyTheKind()
    {
        var map = await Pair.CreateTestMap();
        await Build(map);
        var server = Server;
        var analyzer = server.System<ApiaryAnalyzerSystem>();

        await server.WaitAssertion(() =>
        {
            var comp = server.EntMan.GetComponent<BeehiveComponent>(_hive);
            comp.KnownSources.Add(new BeehiveSource(_human, server.Timing.CurTime));
            var state = analyzer.BuildState(_hive)!;

            Assert.That(state.Trays, Has.Count.EqualTo(1));
            var data = state.Trays[0];
            Assert.That(data.Creature);
            Assert.That(data.Tray, Is.EqualTo(default(NetEntity)), "no entity is sent");
            Assert.That(data.CreatureName, Is.EqualTo("species-name-human"));
            Assert.That(data.CreaturePrototype.ToString(), Is.EqualTo("MobHuman"));
        });
    }

    /// <summary>
    /// A person in a sealed suit with a helmet is no target for the bees: bites do not get through.
    /// </summary>
    [Test]
    public async Task SealedSuitIsNotATarget()
    {
        var map = await Pair.CreateTestMap();
        await Build(map);
        var server = Server;
        var hives = server.System<BeehiveSystem>();

        await server.WaitAssertion(() =>
        {
            var comp = server.EntMan.GetComponent<BeehiveComponent>(_hive);
            var source = new BeehiveSource(_human, server.Timing.CurTime);
            Assert.That(server.System<BeeScoutSystem>().GetTrayState((_hive, comp), source), Is.EqualTo(ApiaryTrayState.Ripe), "bare");

            var inventory = server.System<Content.Shared.Inventory.InventorySystem>();
            var suit = server.EntMan.SpawnEntity("ClothingOuterSuitEmergency", server.EntMan.GetComponent<TransformComponent>(_human).Coordinates);
            var helmet = server.EntMan.SpawnEntity("ClothingHeadHelmetEVA", server.EntMan.GetComponent<TransformComponent>(_human).Coordinates);
            Assert.That(inventory.TryEquip(_human, suit, "outerClothing", force: true));
            Assert.That(inventory.TryEquip(_human, helmet, "head", force: true));

            Assert.That(server.System<BeeScoutSystem>().GetTrayState((_hive, comp), source), Is.EqualTo(ApiaryTrayState.Blind), "suited up");
        });
    }

    /// <summary>
    /// The map holds only so many meat hives: past the limit a swarm that would live on blood is a plant swarm.
    /// </summary>
    [Test]
    public async Task MeatHiveLimitTurnsASwarmIntoAPlantOne()
    {
        var map = await Pair.CreateTestMap();
        await Build(map);
        var server = Server;
        var hives = server.System<BeehiveSystem>();

        await server.WaitPost(() =>
        {
            var grid = map.Grid.Owner;
            server.CfgMan.SetCVar(BeekeepingCVars.MeatHiveLimit, 1);
            server.EntMan.SpawnEntity("ClosetMaintenance", new EntityCoordinates(grid, new Vector2(2.5f, 1.5f)));
            var queen = server.EntMan.SpawnEntity("BeeQueen", new EntityCoordinates(grid, new Vector2(0.5f, 0.5f)));
            var comp = server.EntMan.GetComponent<BeehiveComponent>(_hive);
            Assert.That(server.System<Content.Shared.Containers.ItemSlots.ItemSlotsSystem>().TryInsert(_hive, comp.QueenSlot, queen, null));
            comp.Bees = 10;
            comp.MaxBees = 20;

            // A meat hive already stands on the map.
            var wild = server.EntMan.SpawnEntity("WildBeehive", new EntityCoordinates(grid, new Vector2(5.5f, 0.5f)));
            server.EntMan.GetComponent<BeehiveComponent>(wild).Diet = BeehiveDiet.Meat;
        });
        await RunTicksSync(10);

        await server.WaitAssertion(() =>
        {
            server.EntMan.GetComponent<WeldableComponent>(_entry).IsWelded = true;
            var comp = server.EntMan.GetComponent<BeehiveComponent>(_hive);
            Assert.That(server.System<BeeSwarmSystem>().TrySwarm((_hive, comp)), "the swarm left");

            var swarms = server.EntMan.AllEntityQueryEnumerator<BeeSwarmComponent>();
            Assert.That(swarms.MoveNext(out _, out var swarm));
            Assert.That(swarm!.Diet, Is.EqualTo(BeehiveDiet.Plants), "the limit of meat hives is reached");
        });
    }

    /// <summary>
    /// A meat forager is an NPC: it flies off from the hive by itself, and it can be hit like any small mob.
    /// </summary>
    [Test]
    public async Task ForagerIsAFlyingNpc()
    {
        var map = await Pair.CreateTestMap();
        await Build(map);
        var server = Server;
        EntityUid bee = default;

        await server.WaitPost(() =>
        {
            var comp = server.EntMan.GetComponent<BeehiveComponent>(_hive);
            comp.Bees = 10;
            comp.VisitBudget = 1f;
            server.System<MeatBeehiveSystem>().Hunt((_hive, comp));
            bee = comp.Foragers[0];
            Assert.That(server.EntMan.HasComponent<Content.Server.NPC.HTN.HTNComponent>(bee));
            Assert.That(server.EntMan.HasComponent<Content.Shared.Damage.Components.DamageableComponent>(bee));
        });

        await RunTicksSync(30 * 2);
        await server.WaitAssertion(() =>
        {
            var xforms = server.System<SharedTransformSystem>();
            var moved = (xforms.GetWorldPosition(bee) - xforms.GetWorldPosition(_hive)).Length();
            Assert.That(moved, Is.GreaterThan(0.5f), "it flew off on its own");
        });
    }

    private int CountHums(Robust.Shared.GameObjects.IEntityManager entMan)
    {
        var count = 0;
        var hums = entMan.AllEntityQueryEnumerator<BeeVentHumComponent>();
        while (hums.MoveNext(out _, out _))
        {
            count++;
        }

        return count;
    }

    /// <summary>
    /// While a meat colony lives on the grid, its vents hum; a welded vent and a grid without a colony do not.
    /// </summary>
    [Test]
    public async Task VentsHumWhileAMeatColonyLives()
    {
        var map = await Pair.CreateTestMap();
        await Build(map);
        var server = Server;
        EntityUid wild = default;

        await server.WaitAssertion(() => Assert.That(CountHums(server.EntMan), Is.Zero, "no colony, no hum"));

        await server.WaitPost(() =>
        {
            wild = server.EntMan.SpawnEntity("WildBeehive", new EntityCoordinates(map.Grid.Owner, new Vector2(6.5f, 0.5f)));
            var comp = server.EntMan.GetComponent<BeehiveComponent>(wild);
            comp.Diet = BeehiveDiet.Meat;
            comp.Bees = 5;
        });
        await RunTicksSync(30 * 6);
        await server.WaitAssertion(() => Assert.That(CountHums(server.EntMan), Is.EqualTo(2), "both vents hum"));

        await server.WaitPost(() => server.EntMan.GetComponent<WeldableComponent>(_exit).IsWelded = true);
        await RunTicksSync(30 * 6);
        await server.WaitAssertion(() => Assert.That(CountHums(server.EntMan), Is.EqualTo(1), "the welded vent is quiet"));

        await server.WaitPost(() => server.EntMan.DeleteEntity(wild));
        await RunTicksSync(30 * 6);
        await server.WaitAssertion(() => Assert.That(CountHums(server.EntMan), Is.Zero, "the colony is gone, so is the hum"));
    }

    /// <summary>
    /// What the hum costs the server with fifty vents on the grid: the same stretch of simulation with and without a
    /// meat colony. The numbers go to the test output.
    /// </summary>
    [Test]
    public async Task HumCostWithFiftyVents()
    {
        var map = await Pair.CreateTestMap();
        var server = Server;

        await server.WaitPost(() =>
        {
            var grid = map.Grid.Owner;
            var mapSys = server.System<SharedMapSystem>();
            var plating = new Tile(server.ResolveDependency<ITileDefinitionManager>()["Plating"].TileId);
            for (var x = 0; x < 60; x++)
                mapSys.SetTile(grid, map.Grid.Comp, new Vector2i(x, 0), plating);

            for (var x = 0; x < 50; x++)
                server.EntMan.SpawnEntity("GasVentPump", new EntityCoordinates(grid, new Vector2(x + 0.5f, 0.5f)));
        });
        await RunTicksSync(60);

        const int ticks = 900;
        var watch = System.Diagnostics.Stopwatch.StartNew();
        await RunTicksSync(ticks);
        var without = watch.Elapsed.TotalMilliseconds / ticks;

        await server.WaitPost(() =>
        {
            var wild = server.EntMan.SpawnEntity("WildBeehive", new EntityCoordinates(map.Grid.Owner, new Vector2(55.5f, 0.5f)));
            var comp = server.EntMan.GetComponent<BeehiveComponent>(wild);
            comp.Diet = BeehiveDiet.Meat;
            comp.Bees = 5;
        });
        await RunTicksSync(30 * 6);
        await server.WaitAssertion(() => Assert.That(CountHums(server.EntMan), Is.EqualTo(50)));

        watch.Restart();
        await RunTicksSync(ticks);
        var with = watch.Elapsed.TotalMilliseconds / ticks;

        TestContext.Out.WriteLine($"HUMCOST ms per tick (server and client together): without {without:0.000}, with 50 humming vents {with:0.000}, difference {with - without:0.000}");
    }

    /// <summary>
    /// A meat swarm lands and builds a wild hive that shows its stripes: the appearance says so.
    /// </summary>
    [Test]
    public async Task WildMeatHiveShowsItsStripes()
    {
        var map = await Pair.CreateTestMap();
        await Build(map);
        var server = Server;
        var hives = server.System<BeehiveSystem>();

        await server.WaitPost(() =>
        {
            var grid = map.Grid.Owner;
            server.EntMan.SpawnEntity("ClosetMaintenance", new EntityCoordinates(grid, new Vector2(2.5f, 1.5f)));
            var queen = server.EntMan.SpawnEntity("BeeQueen", new EntityCoordinates(grid, new Vector2(0.5f, 0.5f)));
            var comp = server.EntMan.GetComponent<BeehiveComponent>(_hive);
            Assert.That(server.System<Content.Shared.Containers.ItemSlots.ItemSlotsSystem>().TryInsert(_hive, comp.QueenSlot, queen, null));
            comp.Bees = 10;
            comp.MaxBees = 20;
            server.EntMan.GetComponent<WeldableComponent>(_entry).IsWelded = true;
            Assert.That(server.System<BeeSwarmSystem>().TrySwarm((_hive, comp)), "the swarm left");
        });

        // Wait for the swarm to land and build its hive.
        EntityUid wild = default;
        for (var i = 0; i < 120 && wild == default; i++)
        {
            await RunTicksSync(30);
            await server.WaitPost(() =>
            {
                var query = server.EntMan.AllEntityQueryEnumerator<BeehiveComponent>();
                while (query.MoveNext(out var uid, out var hive))
                {
                    if (hive.Wild)
                        wild = uid;
                }
            });
        }

        Assert.That(wild, Is.Not.EqualTo(default(EntityUid)), "the swarm built a wild hive");
        await server.WaitAssertion(() =>
        {
            var comp = server.EntMan.GetComponent<BeehiveComponent>(wild);
            if (comp.Diet != BeehiveDiet.Meat)
                Assert.Ignore("the draw gave a plant swarm");

            var appearance = server.System<Robust.Shared.GameObjects.SharedAppearanceSystem>();
            Assert.That(appearance.TryGetData<bool>(wild, WildBeehiveVisuals.Meat, out var meat), "the data is set");
            Assert.That(meat);
        });
    }
}
