#nullable enable
using System.Numerics;
using Content.IntegrationTests.Fixtures;
using Content.Server._RedStar.Beekeeping.Systems;
using Content.Shared._RedStar.Beekeeping.Components;
using Content.Shared._RedStar.Beekeeping.Data;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Maths;

namespace Content.IntegrationTests.Tests._RedStar.Beekeeping;

/// <summary>
/// A swarm lives on plants unless there are creatures around and no plants; small creatures count only in a group.
/// </summary>
[TestOf(typeof(BeehiveSystem))]
public sealed class SwarmDietTest : GameTest
{
    [Test]
    public void ChoiceByCounts()
    {
        Assert.Multiple(() =>
        {
            Assert.That(MeatBeehiveSystem.ChooseDiet(0, 0, 0.5f), Is.EqualTo(BeehiveDiet.Plants));
            Assert.That(MeatBeehiveSystem.ChooseDiet(3, 0, 0f), Is.EqualTo(BeehiveDiet.Plants));
            Assert.That(MeatBeehiveSystem.ChooseDiet(0, 2, 0.99f), Is.EqualTo(BeehiveDiet.Meat));

            // Both: three creatures to one plant is meat for three rolls in four.
            Assert.That(MeatBeehiveSystem.ChooseDiet(1, 3, 0.74f), Is.EqualTo(BeehiveDiet.Meat));
            Assert.That(MeatBeehiveSystem.ChooseDiet(1, 3, 0.76f), Is.EqualTo(BeehiveDiet.Plants));
        });
    }

    [Test]
    public async Task CreaturesAroundTheExit()
    {
        var server = Server;
        var map = await Pair.CreateTestMap();
        var hiveSystem = server.System<BeehiveSystem>();
        var hive = default(EntityUid);
        var center = default(MapCoordinates);

        await server.WaitPost(() =>
        {
            var grid = map.Grid.Owner;
            hive = server.EntMan.SpawnEntity("Beehive", new EntityCoordinates(grid, new Vector2(0.5f, 0.5f)));
            center = server.System<SharedTransformSystem>().GetMapCoordinates(hive);
        });

        await server.WaitAssertion(() =>
        {
            var comp = server.EntMan.GetComponent<BeehiveComponent>(hive);
            Assert.That(server.System<MeatBeehiveSystem>().PickSwarmDiet((hive, comp), center), Is.EqualTo(BeehiveDiet.Plants), "nothing around");
        });

        // One mouse is too small to count.
        await server.WaitPost(() => server.EntMan.SpawnEntity("MobMouse", new EntityCoordinates(map.Grid.Owner, new Vector2(2.5f, 0.5f))));
        await RunTicksSync(5);
        await server.WaitAssertion(() =>
        {
            var comp = server.EntMan.GetComponent<BeehiveComponent>(hive);
            Assert.That(server.System<MeatBeehiveSystem>().PickSwarmDiet((hive, comp), center), Is.EqualTo(BeehiveDiet.Plants), "one mouse");
        });

        // Two do.
        await server.WaitPost(() => server.EntMan.SpawnEntity("MobMouse", new EntityCoordinates(map.Grid.Owner, new Vector2(3.5f, 0.5f))));
        await RunTicksSync(5);
        await server.WaitAssertion(() =>
        {
            var comp = server.EntMan.GetComponent<BeehiveComponent>(hive);
            Assert.That(server.System<MeatBeehiveSystem>().PickSwarmDiet((hive, comp), center), Is.EqualTo(BeehiveDiet.Meat), "two mice");
        });
    }

    /// <summary>
    /// With several places to settle at, creatures around any of them count: the draw does not depend on which place
    /// it happened to look at first.
    /// </summary>
    [Test]
    public async Task DietFollowsAllThePlacesItCanSettleAt()
    {
        var server = Server;
        var map = await Pair.CreateTestMap();
        var hiveSystem = server.System<BeehiveSystem>();
        var hive = default(EntityUid);
        MapCoordinates empty = default;
        MapCoordinates crowded = default;

        await server.WaitPost(() =>
        {
            var grid = map.Grid.Owner;
            var mapSys = server.System<SharedMapSystem>();
            var plating = new Tile(server.ResolveDependency<ITileDefinitionManager>()["Plating"].TileId);
            for (var x = 0; x <= 40; x++)
                mapSys.SetTile(grid, map.Grid.Comp, new Vector2i(x, 0), plating);

            hive = server.EntMan.SpawnEntity("Beehive", new EntityCoordinates(grid, new Vector2(0.5f, 0.5f)));
            var transform = server.System<SharedTransformSystem>();
            empty = transform.ToMapCoordinates(new EntityCoordinates(grid, new Vector2(2.5f, 0.5f)));
            crowded = transform.ToMapCoordinates(new EntityCoordinates(grid, new Vector2(35.5f, 0.5f)));
            server.EntMan.SpawnEntity("MobMouse", new EntityCoordinates(grid, new Vector2(35.5f, 0.5f)));
            server.EntMan.SpawnEntity("MobMouse", new EntityCoordinates(grid, new Vector2(36.5f, 0.5f)));
        });
        await RunTicksSync(5);

        await server.WaitAssertion(() =>
        {
            var comp = server.EntMan.GetComponent<BeehiveComponent>(hive);
            // Only the empty place: a plain plant swarm. With the crowded one among the places: meat.
            Assert.That(server.System<MeatBeehiveSystem>().PickSwarmDietAmong((hive, comp), new[] { empty }), Is.EqualTo(BeehiveDiet.Plants));
            Assert.That(server.System<MeatBeehiveSystem>().PickSwarmDietAmong((hive, comp), new[] { empty, crowded }), Is.EqualTo(BeehiveDiet.Meat));
        });
    }
}
