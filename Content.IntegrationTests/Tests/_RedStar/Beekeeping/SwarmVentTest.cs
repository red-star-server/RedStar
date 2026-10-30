#nullable enable
using System.Numerics;
using Content.IntegrationTests.Fixtures;
using Content.Server._RedStar.Beekeeping.Systems;
using Content.Shared.Tools.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Maths;

namespace Content.IntegrationTests.Tests._RedStar.Beekeeping;

/// <summary>
/// A swarm picks a vent it can see and an exit vent in the same pipe network; a welded vent does not count.
/// </summary>
[TestOf(typeof(BeehiveSystem))]
public sealed class SwarmVentTest : GameTest
{
    [Test]
    public async Task VentRouteAndWelding()
    {
        var server = Server;
        var map = await Pair.CreateTestMap();
        var hiveSystem = server.System<BeehiveSystem>();
        EntityUid hive = default;
        EntityUid entry = default;
        EntityUid exit = default;

        await server.WaitPost(() =>
        {
            var grid = map.Grid.Owner;
            // Floor under the pipes.
            var mapSys = server.System<SharedMapSystem>();
            var plating = new Tile(server.ResolveDependency<ITileDefinitionManager>()["Plating"].TileId);
            for (var x = 0; x <= 6; x++)
            {
                for (var y = 0; y <= 1; y++)
                    mapSys.SetTile(grid, map.Grid.Comp, new Vector2i(x, y), plating);
            }

            EntityCoordinates At(float x, float y) => new(grid, new Vector2(x + 0.5f, y + 0.5f));

            hive = server.EntMan.SpawnEntity("Beehive", At(0, 0));
            // Two vents (each connects to the pipe on its south side) joined by a short run of pipe.
            entry = server.EntMan.SpawnEntity("GasVentPump", At(2, 1));
            exit = server.EntMan.SpawnEntity("GasVentPump", At(5, 1));
            server.EntMan.SpawnEntity("GasPipeFourway", At(2, 0));
            server.EntMan.SpawnEntity("GasPipeFourway", At(3, 0));
            server.EntMan.SpawnEntity("GasPipeFourway", At(4, 0));
            server.EntMan.SpawnEntity("GasPipeFourway", At(5, 0));
        });
        await RunTicksSync(20);

        await server.WaitAssertion(() =>
        {
            var hiveXform = server.EntMan.GetComponent<TransformComponent>(hive);
            var grid = hiveXform.GridUid!.Value;
            var origin = server.System<SharedTransformSystem>().GetMapCoordinates(hive, hiveXform);
            var route = server.System<BeeSwarmSystem>().FindSwarmVentRoute(hive, grid, origin, 12f);
            Assert.That(route, Is.Not.Null);
            Assert.That(new[] { entry, exit }, Does.Contain(route!.Value.Entry));
            Assert.That(new[] { entry, exit }, Does.Contain(route.Value.Exit));
            Assert.That(route.Value.Entry, Is.Not.EqualTo(route.Value.Exit));

            // Welding one end shuts the route.
            server.EntMan.GetComponent<WeldableComponent>(exit).IsWelded = true;
            Assert.That(server.System<BeeSwarmSystem>().FindSwarmVentRoute(hive, grid, origin, 12f), Is.Null);
        });
    }

    /// <summary>
    /// A swarm only comes out of a vent that has a place to nest near it: a locker or crate in clear sight.
    /// </summary>
    [Test]
    public async Task ExitsNeedANestNearby()
    {
        var server = Server;
        var map = await Pair.CreateTestMap();
        var hiveSystem = server.System<BeehiveSystem>();
        EntityUid entry = default;
        EntityUid bare = default;
        EntityUid withCloset = default;

        await server.WaitPost(() =>
        {
            var grid = map.Grid.Owner;
            var mapSys = server.System<SharedMapSystem>();
            var plating = new Tile(server.ResolveDependency<ITileDefinitionManager>()["Plating"].TileId);
            for (var x = 0; x <= 20; x++)
            {
                for (var y = 0; y <= 2; y++)
                    mapSys.SetTile(grid, map.Grid.Comp, new Vector2i(x, y), plating);
            }

            EntityCoordinates At(float x, float y) => new(grid, new Vector2(x + 0.5f, y + 0.5f));

            entry = server.EntMan.SpawnEntity("GasVentPump", At(1, 1));
            bare = server.EntMan.SpawnEntity("GasVentPump", At(8, 1));
            withCloset = server.EntMan.SpawnEntity("GasVentPump", At(18, 1));
            for (var x = 1; x <= 18; x++)
                server.EntMan.SpawnEntity("GasPipeFourway", At(x, 0));

            server.EntMan.SpawnEntity("ClosetMaintenance", At(18, 2));
        });
        await RunTicksSync(20);

        await server.WaitAssertion(() =>
        {
            var grid = server.EntMan.GetComponent<TransformComponent>(entry).GridUid!.Value;
            var exits = server.System<BeeSwarmSystem>().NestExits(entry, 5f, grid);
            Assert.That(exits, Is.EquivalentTo(new[] { withCloset }), "only the vent beside the closet");
            Assert.That(exits, Does.Not.Contain(bare));
        });
    }
}
