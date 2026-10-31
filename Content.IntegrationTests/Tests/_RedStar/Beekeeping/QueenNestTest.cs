#nullable enable
using System.Linq;
using Content.IntegrationTests.Fixtures;
using Content.Shared._RedStar.Beekeeping.Components;
using Content.Shared.Containers.ItemSlots;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;

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
