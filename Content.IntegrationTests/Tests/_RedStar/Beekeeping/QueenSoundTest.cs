#nullable enable
using System.Numerics;
using Content.IntegrationTests.Fixtures;
using Content.Server._RedStar.Beekeeping.Components;
using Content.Shared.Audio;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Maths;

namespace Content.IntegrationTests.Tests._RedStar.Beekeeping;

/// <summary>
/// A queen put down in the world buzzes with the flying sound while she flies to a hive.
/// </summary>
public sealed class QueenSoundTest : GameTest
{
    [Test]
    public async Task QueenFlyingToAHiveBuzzesLikeAFlyingBee()
    {
        var server = Server;
        var map = await Pair.CreateTestMap();
        EntityUid queen = default;

        await server.WaitPost(() =>
        {
            var mapSys = server.System<SharedMapSystem>();
            var plating = new Tile(server.ResolveDependency<ITileDefinitionManager>()["Plating"].TileId);
            for (var x = 0; x <= 8; x++)
                mapSys.SetTile(map.Grid.Owner, map.Grid.Comp, new Vector2i(x, 0), plating);

            server.EntMan.SpawnEntity("Beehive", new EntityCoordinates(map.Grid.Owner, new Vector2(6.5f, 0.5f)));
            queen = server.EntMan.SpawnEntity("BeeQueen", new EntityCoordinates(map.Grid.Owner, new Vector2(1.5f, 0.5f)));
        });
        await RunTicksSync(20);

        await server.WaitAssertion(() =>
        {
            var homing = server.EntMan.GetComponent<BeeHomingComponent>(queen);
            var ambient = server.EntMan.GetComponent<AmbientSoundComponent>(queen);
            var path = (ambient.Sound as SoundPathSpecifier)?.Path.ToString();
            Assert.That(homing.Flying, "she flies");
            Assert.That(ambient.Enabled, "the sound is on");
            Assert.That(path, Does.Contain("bee_flying"), "the flying sound, not the one for a hand");
        });
    }

    /// <summary>
    /// A queen with no hive flies off in a flyer of her own and rides inside it: the flyer buzzes, she does not, or the
    /// sound of a bee in a hand would be heard from the flying cloud.
    /// </summary>
    [Test]
    public async Task QueenRidingInAFlyerIsQuiet()
    {
        var server = Server;
        var map = await Pair.CreateTestMap();
        EntityUid queen = default;

        await server.WaitPost(() => queen = server.EntMan.SpawnEntity("BeeQueen", map.GridCoords));
        await RunTicksSync(30);

        await server.WaitAssertion(() =>
        {
            var inside = server.System<Robust.Shared.Containers.SharedContainerSystem>().TryGetContainingContainer(queen, out var holder);
            Assert.That(inside, "she rides in a flyer");
            Assert.That(server.EntMan.HasComponent<BeeSwarmComponent>(holder!.Owner));
            Assert.That(server.EntMan.GetComponent<AmbientSoundComponent>(queen).Enabled, Is.False, "she is quiet inside");
            Assert.That(server.EntMan.GetComponent<AmbientSoundComponent>(holder.Owner).Enabled, Is.True, "the flyer buzzes");
        });
    }
}
