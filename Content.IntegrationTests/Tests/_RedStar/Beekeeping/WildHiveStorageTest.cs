#nullable enable
using Content.IntegrationTests.Fixtures;
using Content.Shared.Storage.Components;
using Content.Shared.Storage.EntitySystems;
using Content.Shared._RedStar.Beekeeping.Components;
using Robust.Shared.GameObjects;

namespace Content.IntegrationTests.Tests._RedStar.Beekeeping;

/// <summary>
/// A wild hive lying in an open locker goes inside when the locker is closed, like any other thing in it.
/// </summary>
[TestOf(typeof(BeehiveComponent))]
public sealed class WildHiveStorageTest : GameTest
{
    [Test]
    public async Task ClosingTakesWildHive()
    {
        var server = Server;
        var map = await Pair.CreateTestMap();
        var storage = server.System<SharedEntityStorageSystem>();
        EntityUid closet = default;
        EntityUid wild = default;

        await server.WaitPost(() =>
        {
            closet = server.EntMan.SpawnEntity("ClosetMaintenance", map.GridCoords);
            storage.OpenStorage(closet);
            wild = server.EntMan.SpawnEntity("WildBeehive", map.GridCoords);
        });
        await RunTicksSync(5);

        await server.WaitPost(() => storage.CloseStorage(closet));
        await RunTicksSync(5);

        await server.WaitAssertion(() =>
            Assert.That(server.EntMan.GetComponent<EntityStorageComponent>(closet).Contents.Contains(wild)));
    }
}
