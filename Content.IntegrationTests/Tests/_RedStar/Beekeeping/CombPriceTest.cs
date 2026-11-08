#nullable enable
using Content.IntegrationTests.Fixtures;
using Content.Server.Cargo.Systems;
using Content.Shared.Stacks;

namespace Content.IntegrationTests.Tests._RedStar.Beekeeping;

/// <summary>
/// A stack of combs sells for as many combs as are in it. The reagents inside are priced once per stack (a stack holds
/// one comb's worth), so only the per-comb part is checked to grow with the count.
/// </summary>
[TestOf(typeof(PricingSystem))]
public sealed class CombPriceTest : GameTest
{
    /// <summary>
    /// Правило: каждая сота в стопке добавляет к цене 50, так что стопка из 4 сот дороже одной на 150.
    /// </summary>
    [Test]
    public async Task StackOfCombsSellsPerComb()
    {
        var map = await Pair.CreateTestMap();
        await Server.WaitAssertion(() =>
        {
            var pricing = Server.System<PricingSystem>();
            var one = Server.EntMan.SpawnEntity("FoodHoneycomb", map.GridCoords);
            var four = Server.EntMan.SpawnEntity("FoodHoneycomb", map.GridCoords);
            Server.System<SharedStackSystem>().SetCount((four, Server.EntMan.GetComponent<StackComponent>(four)), 4);

            var single = pricing.GetPrice(one);
            Assert.Multiple(() =>
            {
                Assert.That(single, Is.InRange(50, 70), "one comb");
                Assert.That(pricing.GetPrice(four) - single, Is.EqualTo(150).Within(0.01), "three more combs in the stack");
            });
        });
    }
}
