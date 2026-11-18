#nullable enable
using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.Server._RedStar.Beekeeping.Components;
using Content.Server._RedStar.Beekeeping.Systems;
using Content.Shared._RedStar.Beekeeping;
using Content.Shared._RedStar.Beekeeping.Components;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Storage.Components;
using Content.Shared.Storage.EntitySystems;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;

namespace Content.IntegrationTests.Tests._RedStar.Beekeeping;

[TestOf(typeof(BeeSwarmSystem))]
public sealed class WildHiveLimitTest : GameTest
{
    [Test]
    public async Task SwarmLeavesTheNewHiveOutsideAnOccupiedLocker()
    {
        var server = Server;
        var map = await Pair.CreateTestMap();
        var swarms = server.System<BeeSwarmSystem>();
        var containers = server.System<SharedContainerSystem>();
        var slots = server.System<ItemSlotsSystem>();
        var storage = server.System<SharedEntityStorageSystem>();

        await server.WaitPost(() =>
        {
            var entities = server.EntMan;
            var grid = map.Grid.Owner;
            var maps = server.System<SharedMapSystem>();
            maps.SetTile(grid, map.Grid.Comp, new Vector2i(1, 0), map.Tile.Tile);
            maps.SetTile(grid, map.Grid.Comp, new Vector2i(2, 0), map.Tile.Tile);
            var locker = entities.SpawnAttachedTo("ClosetMaintenance", new EntityCoordinates(grid, 2.5f, 0.5f));
            var hive = entities.SpawnAttachedTo("Beehive", map.GridCoords);
            var queen = entities.SpawnAttachedTo("BeeQueen", map.GridCoords);
            var hiveComp = entities.GetComponent<BeehiveComponent>(hive);
            Assert.That(slots.TryInsert(hive, hiveComp.QueenSlot, queen, null), Is.True);
            Assert.That(swarms.TryAbandon((hive, hiveComp)), Is.True);
            Assert.That(containers.TryGetContainingContainer(queen, out var swarmContainer), Is.True);
            var swarmUid = swarmContainer!.Owner;
            var swarm = entities.GetComponent<BeeSwarmComponent>(swarmUid);
            Assert.That(swarm.Storage, Is.EqualTo(locker));

            var existingHive = entities.SpawnAttachedTo("WildBeehive", map.GridCoords);
            Assert.That(storage.Insert(existingHive, locker), Is.True);
            swarms.LandSwarm((swarmUid, swarm));

            Assert.That(entities.GetComponent<EntityStorageComponent>(locker).Contents.ContainedEntities,
                Is.EqualTo(new[] { existingHive }));
            Assert.That(containers.TryGetContainingContainer(queen, out var newHive), Is.True);
            Assert.That(entities.HasComponent<BeehiveComponent>(newHive!.Owner), Is.True);
            Assert.That(containers.IsEntityInContainer(newHive.Owner), Is.False);
        });
    }

    [Test]
    public async Task OnlyOneOfTwoAirborneSwarmsCanNestAtLimitOne()
    {
        var server = Server;
        var map = await Pair.CreateTestMap();
        var swarms = server.System<BeeSwarmSystem>();
        var slots = server.System<ItemSlotsSystem>();
        var containers = server.System<SharedContainerSystem>();
        await OverrideCVar(Side.Server, BeekeepingCVars.WildHiveLimit, 1);

        await server.WaitPost(() =>
        {
            var first = LaunchSwarm();
            var second = LaunchSwarm();
            Assert.That(swarms.CountWildHives(map.MapUid), Is.Zero);

            swarms.LandSwarm(first.Swarm);
            swarms.LandSwarm(second.Swarm);

            Assert.Multiple(() =>
            {
                Assert.That(swarms.CountWildHives(map.MapUid), Is.EqualTo(1));
                Assert.That(containers.TryGetContainingContainer(first.Queen, out _), Is.True);
                Assert.That(server.EntMan.EntityExists(second.Queen), Is.True);
                Assert.That(containers.TryGetContainingContainer(second.Queen, out _), Is.False);
            });
        });

        (Entity<BeeSwarmComponent> Swarm, EntityUid Queen) LaunchSwarm()
        {
            var hive = server.EntMan.SpawnAttachedTo("Beehive", map.GridCoords);
            var queen = server.EntMan.SpawnAttachedTo("BeeQueen", map.GridCoords);
            var hiveComp = server.EntMan.GetComponent<BeehiveComponent>(hive);
            Assert.That(slots.TryInsert(hive, hiveComp.QueenSlot, queen, null), Is.True);
            Assert.That(swarms.TryAbandon((hive, hiveComp)), Is.True);
            Assert.That(containers.TryGetContainingContainer(queen, out var container), Is.True);

            var swarm = container!.Owner;
            return (new Entity<BeeSwarmComponent>(swarm, server.EntMan.GetComponent<BeeSwarmComponent>(swarm)), queen);
        }
    }
}
