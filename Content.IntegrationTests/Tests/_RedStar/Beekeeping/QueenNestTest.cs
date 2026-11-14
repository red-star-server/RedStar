#nullable enable
using System.Linq;
using Content.IntegrationTests.Fixtures;
using Content.Server._RedStar.Beekeeping.Components;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Storage.Components;
using Content.Shared._RedStar.Beekeeping.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Spawners;
using Robust.Shared.Containers;

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
        var map = await Pair.CreateTestMap();
        var slots = server.System<ItemSlotsSystem>();
        EntityUid hive = default;
        EntityUid queen = default;

        await server.WaitPost(() =>
        {
            hive = server.EntMan.SpawnAttachedTo("Beehive", map.GridCoords);
            queen = server.EntMan.SpawnAttachedTo("BeeQueen", map.GridCoords);
            Assert.That(server.EntMan.GetComponent<BeeQueenComponent>(queen).DiesAt, Is.Not.Null);
            var comp = server.EntMan.GetComponent<BeehiveComponent>(hive);
            Assert.That(slots.TryInsert(hive, comp.QueenSlot, queen, null), Is.True);
        });
        await RunTicksSync(5);

        await server.WaitPost(() =>
        {
            var queenComp = server.EntMan.GetComponent<BeeQueenComponent>(queen);
            Assert.That(queenComp.DiesAt, Is.Null);
            Assert.That(server.EntMan.HasComponent<TimedDespawnComponent>(queen), Is.False);

            queenComp.Lifetime = TimeSpan.FromSeconds(1);
            var hiveComp = server.EntMan.GetComponent<BeehiveComponent>(hive);
            Assert.That(slots.TryEject(hive, hiveComp.QueenSlot, null, out var ejected), Is.True);
            Assert.That(ejected, Is.EqualTo(queen));
            Assert.That(queenComp.DiesAt, Is.Not.Null);
            // A queen on the floor would otherwise try to return to her old hive.
            server.EntMan.DeleteEntity(hive);
        });
        await RunSeconds(4);

        await server.WaitAssertion(() => Assert.That(server.EntMan.Deleted(queen)));
    }

    [Test]
    public async Task SearchingQueenKeepsHerLifetime()
    {
        var server = Server;
        var map = await Pair.CreateTestMap();
        EntityUid queen = default;
        await server.WaitPost(() => queen = server.EntMan.SpawnAttachedTo("BeeQueen", map.GridCoords));

        await RunSeconds(3);
        await server.WaitAssertion(() =>
        {
            var containers = server.System<SharedContainerSystem>();
            Assert.That(containers.TryGetContainingContainer(queen, out var container), Is.True);
            Assert.That(server.EntMan.GetComponent<BeeSwarmComponent>(container!.Owner).Searching, Is.True);
            Assert.That(server.EntMan.GetComponent<BeeQueenComponent>(queen).DiesAt, Is.Not.Null);
            Assert.That(server.EntMan.HasComponent<TimedDespawnComponent>(queen), Is.True);
        });
    }

    [Test]
    public async Task InvalidNestFlyerIsRemoved()
    {
        var server = Server;
        var map = await Pair.CreateTestMap();
        EntityUid queen = default;
        await server.WaitPost(() =>
        {
            server.EntMan.SpawnAttachedTo("ClosetMaintenance", map.GridCoords);
            queen = server.EntMan.SpawnAttachedTo("BeeQueen", map.GridCoords);
            server.EntMan.GetComponent<BeeQueenComponent>(queen).NestFlyerPrototype = "BeePollinator";
        });

        await RunTicksSync(3);
        await server.WaitAssertion(() =>
        {
            Assert.That(server.EntMan.GetComponent<BeeHomingComponent>(queen).NextNestTry, Is.GreaterThan(TimeSpan.Zero));
            Assert.That(server.EntMan.EntityQuery<BeeErrandComponent>(), Is.Empty);
        });
    }
}
