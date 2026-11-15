#nullable enable
using System.Linq;
using Content.IntegrationTests.Fixtures;
using Content.Shared.Containers.ItemSlots;
using Content.Shared._RedStar.Beekeeping.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Spawners;

namespace Content.IntegrationTests.Tests._RedStar.Beekeeping;

/// <summary>
/// A queen let go with no hive to go back to flies off and founds a wild hive in a closet nearby.
/// </summary>
[TestOf(typeof(BeeQueenComponent))]
public sealed class QueenNestTest : GameTest
{
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
            server.EntMan.DeleteEntity(hive);
        });
        await RunSeconds(4);

        await server.WaitAssertion(() => Assert.That(server.EntMan.Deleted(queen)));
    }
}
