#nullable enable
using Content.IntegrationTests.Fixtures;
using Content.Server._RedStar.Beekeeping.Systems;
using Content.Shared.Containers.ItemSlots;
using Content.Shared._RedStar.Beekeeping.Components;
using Content.Shared._RedStar.Beekeeping.Data;
using Robust.Shared.GameObjects;

namespace Content.IntegrationTests.Tests._RedStar.Beekeeping;

/// <summary>
/// The apiary analyzer reads a hive with its frames and queen, a queen on her own and a bee item, and refuses
/// anything else.
/// </summary>
[TestOf(typeof(ApiaryAnalyzerSystem))]
public sealed class ApiaryAnalyzerTest : GameTest
{
    [Test]
    public async Task ScanStates()
    {
        var server = Server;
        var map = await Pair.CreateTestMap();
        var slots = server.System<ItemSlotsSystem>();
        var analyzer = server.System<ApiaryAnalyzerSystem>();

        await server.WaitAssertion(() =>
        {
            var hive = server.EntMan.SpawnEntity("Beehive", map.GridCoords);
            var comp = server.EntMan.GetComponent<BeehiveComponent>(hive);
            Assert.That(slots.TryInsert(hive, comp.FrameSlots[0], server.EntMan.SpawnEntity("BeeFrame", map.GridCoords), null));
            var big = server.EntMan.SpawnEntity("BeeFrame", map.GridCoords);
            server.EntMan.GetComponent<BeeFrameComponent>(big).MaxCombs = 4;
            Assert.That(slots.TryInsert(hive, comp.FrameSlots[1], big, null));

            var empty = analyzer.BuildState(hive);
            Assert.That(empty, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(empty!.Kind, Is.EqualTo(ApiaryScanKind.Hive));
                Assert.That(empty.HasQueen, Is.False);
                Assert.That(empty.Traits, Is.Empty);
                Assert.That(empty.Frames, Has.Count.EqualTo(2));
                Assert.That(empty.MaxCombs, Is.EqualTo(5));
                Assert.That(empty.MaxHealth, Is.GreaterThan(0f));
            });

            var queen = server.EntMan.SpawnEntity("BeeQueen", map.GridCoords);
            var alone = analyzer.BuildState(queen);
            Assert.That(alone, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(alone!.Kind, Is.EqualTo(ApiaryScanKind.Queen));
                Assert.That(alone.HasQueen);
                Assert.That(alone.Traits, Is.EquivalentTo(new[] { "PestControl" }));
                Assert.That(alone.Slots, Is.EqualTo(1));
                Assert.That(alone.UsedSlots, Is.Zero);
            });

            Assert.That(slots.TryInsert(hive, comp.QueenSlot, queen, null));
            var full = analyzer.BuildState(hive);
            Assert.Multiple(() =>
            {
                Assert.That(full!.HasQueen);
                Assert.That(full.Traits, Is.EquivalentTo(new[] { "PestControl" }));
                Assert.That(full.Bees, Is.EqualTo(comp.Bees));
                Assert.That(full.MaxBees, Is.EqualTo(comp.MaxBees));
            });

            var bee = server.EntMan.SpawnEntity("BeePollinator", map.GridCoords);
            var pollinator = analyzer.BuildState(bee);
            Assert.Multiple(() =>
            {
                Assert.That(pollinator!.Kind, Is.EqualTo(ApiaryScanKind.Pollinator));
                Assert.That(pollinator.Charges, Is.EqualTo(server.EntMan.GetComponent<BeeItemComponent>(bee).Charges));
            });

            Assert.That(analyzer.BuildState(server.EntMan.SpawnEntity("BeeFrame", map.GridCoords)), Is.Null);
        });
    }
}
