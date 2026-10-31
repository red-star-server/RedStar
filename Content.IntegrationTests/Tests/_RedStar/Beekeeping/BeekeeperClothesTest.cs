#nullable enable
using Content.IntegrationTests.Fixtures;
using Content.Server._RedStar.Beekeeping.Systems;
using Content.Shared.Inventory;
using Content.Shared.NPC.Systems;
using Robust.Shared.GameObjects;

namespace Content.IntegrationTests.Tests._RedStar.Beekeeping;

/// <summary>
/// The beekeeper suit and hat count as a sealed suit: angry bees leave the wearer alone, but not with the suit only.
/// </summary>
[TestOf(typeof(BeehiveSystem))]
public sealed class BeekeeperClothesTest : GameTest
{
    [Test]
    public async Task SuitAndHatAreSealed()
    {
        var server = Server;
        var map = await Pair.CreateTestMap();
        var inventory = server.System<InventorySystem>();
        var faction = server.System<NpcFactionSystem>();
        EntityUid human = default;
        EntityUid bee = default;

        await server.WaitAssertion(() =>
        {
            human = server.EntMan.SpawnEntity("MobHuman", map.GridCoords);
            bee = server.EntMan.SpawnEntity("MobBeehiveAngryBee", map.GridCoords);
            faction.AggroEntity(bee, human);
            var suit = server.EntMan.SpawnEntity("ClothingOuterSuitBeekeeper", map.GridCoords);
            Assert.That(inventory.TryEquip(human, suit, "outerClothing", force: true));
        });

        await RunTicksSync(30);

        await server.WaitAssertion(() =>
        {
            Assert.That(faction.IsIgnored(bee, human), Is.False, "the suit alone is not sealed");
            var hat = server.EntMan.SpawnEntity("ClothingHeadHatBeekeeper", map.GridCoords);
            Assert.That(inventory.TryEquip(human, hat, "head", force: true));
        });

        await RunTicksSync(30);

        await server.WaitAssertion(() =>
        {
            Assert.That(faction.IsIgnored(bee, human), "suit and hat together are sealed");
        });
    }
}
