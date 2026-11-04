#nullable enable
using System.Linq;
using Content.IntegrationTests.Fixtures;
using Content.Shared._RedStar.Beekeeping.Components;
using Content.Shared.Destructible;
using Content.Shared.Stacks;

namespace Content.IntegrationTests.Tests._RedStar.Beekeeping;

/// <summary>
/// A frame taken apart or broken with combs on it leaves the combs behind.
/// </summary>
[TestOf(typeof(BeeFrameComponent))]
public sealed class FrameCombsTest : GameTest
{
    [Test]
    public async Task BrokenFrameDropsCombs()
    {
        var server = Server;
        var map = await Pair.CreateTestMap();

        await server.WaitPost(() =>
        {
            var frame = server.EntMan.SpawnEntity("BeeFrameCapped", map.GridCoords);
            Assert.That(server.EntMan.GetComponent<BeeFrameComponent>(frame).Combs, Is.EqualTo(1));
            server.System<SharedDestructibleSystem>().DestroyEntity(frame);
        });
        await RunTicksSync(5);

        await server.WaitAssertion(() =>
        {
            var combs = server.EntMan.EntityQuery<StackComponent>()
                .Where(stack => stack.StackTypeId == "Honeycomb")
                .Sum(stack => stack.Count);
            Assert.That(combs, Is.EqualTo(1));
        });
    }
}
