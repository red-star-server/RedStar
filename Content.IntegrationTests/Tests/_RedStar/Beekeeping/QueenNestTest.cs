#nullable enable
using System.Linq;
using Content.IntegrationTests.Fixtures;
using Content.Server._RedStar.Beekeeping.Components;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Storage.Components;
using Content.Shared._RedStar.Beekeeping.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;

namespace Content.IntegrationTests.Tests._RedStar.Beekeeping;

/// <summary>
/// A queen let go with no hive to go back to flies off and founds a wild hive in a closet nearby.
/// </summary>
[TestOf(typeof(BeeQueenComponent))]
public sealed class QueenNestTest : GameTest
{
    [Test]
    public async Task LooseQueenNests()
    {
        var server = Server;
        var map = await Pair.CreateTestMap();
        EntityUid closet = default;

        await server.WaitPost(() =>
        {
            closet = server.EntMan.SpawnEntity("ClosetMaintenance", map.GridCoords);
            server.EntMan.SpawnEntity("BeeQueen", map.GridCoords);
        });

        // The flight is short: up, across nothing, into the closet.
        await RunTicksSync(300);

        await server.WaitAssertion(() =>
        {
            var wildEntity = server.EntMan.AllEntities<BeehiveComponent>().Single();
            var wild = wildEntity.Comp;
            Assert.That(wild, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(wild!.Wild);
                Assert.That(wild.Bees, Is.EqualTo(wild.StartingBees));
                Assert.That(server.EntMan.GetComponent<EntityStorageComponent>(closet).Contents.Contains(wildEntity.Owner));
                Assert.That(server.EntMan.EntityQuery<BeeSwarmComponent>().Count(), Is.Zero);
            });
        });
    }

    /// <summary>
    /// A closet that already holds a wild hive is taken: a second queen does not move in but goes searching.
    /// </summary>
    [Test]
    public async Task TakenClosetIsSkipped()
    {
        var server = Server;
        var map = await Pair.CreateTestMap();

        await server.WaitPost(() =>
        {
            server.EntMan.SpawnEntity("ClosetMaintenance", map.GridCoords);
            server.EntMan.SpawnEntity("WildBeehive", map.GridCoords);
            server.EntMan.SpawnEntity("BeeQueen", map.GridCoords);
        });
        await RunSeconds(3);

        await server.WaitAssertion(() =>
        {
            Assert.That(server.EntMan.EntityQuery<BeehiveComponent>().Count(), Is.EqualTo(1));
            var swarms = server.EntMan.EntityQuery<BeeSwarmComponent>().ToList();
            Assert.That(swarms, Has.Count.EqualTo(1));
            Assert.That(swarms[0].Searching);
        });
    }

    /// <summary>
    /// A fresh queen let go next to an empty hive goes into it rather than off to a closet.
    /// </summary>
    [Test]
    public async Task LooseQueenTakesEmptyHive()
    {
        var server = Server;
        var map = await Pair.CreateTestMap();
        EntityUid hive = default;
        EntityUid queen = default;

        await server.WaitPost(() =>
        {
            server.EntMan.SpawnEntity("ClosetMaintenance", map.GridCoords);
            hive = server.EntMan.SpawnEntity("Beehive", map.GridCoords);
            queen = server.EntMan.SpawnEntity("BeeQueen", map.GridCoords);
        });
        await RunSeconds(3);

        await server.WaitAssertion(() =>
        {
            var comp = server.EntMan.GetComponent<BeehiveComponent>(hive);
            Assert.That(server.System<ItemSlotsSystem>().GetItemOrNull(hive, comp.QueenSlot), Is.EqualTo(queen));
            Assert.That(server.EntMan.EntityQuery<BeehiveComponent>().Count(), Is.EqualTo(1));
        });
    }

    /// <summary>
    /// Out of a hive the queen's clock runs; in a hive it stops; when it runs out she dies.
    /// </summary>
    [Test]
    public async Task QueenLifetime()
    {
        var server = Server;
        var slots = server.System<ItemSlotsSystem>();
        EntityUid hive = default;
        EntityUid queen = default;

        await server.WaitPost(() =>
        {
            hive = server.EntMan.SpawnEntity("Beehive", MapCoordinates.Nullspace);
            queen = server.EntMan.SpawnEntity("BeeQueen", MapCoordinates.Nullspace);
        });
        await RunSeconds(2);

        await server.WaitPost(() =>
        {
            Assert.That(server.EntMan.GetComponent<BeeQueenComponent>(queen).DiesAt, Is.Not.Null);
            var comp = server.EntMan.GetComponent<BeehiveComponent>(hive);
            Assert.That(slots.TryInsert(hive, comp.QueenSlot, queen, null));
        });
        await RunSeconds(2);

        await server.WaitPost(() =>
        {
            Assert.That(server.EntMan.GetComponent<BeeQueenComponent>(queen).DiesAt, Is.Null);
            var comp = server.EntMan.GetComponent<BeehiveComponent>(hive);
            slots.TryEject(hive, comp.QueenSlot, null, out _);
            server.EntMan.GetComponent<BeeQueenComponent>(queen).Lifetime = TimeSpan.FromSeconds(1);
        });
        await RunSeconds(4);

        await server.WaitAssertion(() => Assert.That(server.EntMan.Deleted(queen)));
    }
}
