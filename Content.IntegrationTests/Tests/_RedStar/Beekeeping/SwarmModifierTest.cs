#nullable enable
using System.Linq;
using Content.IntegrationTests.Fixtures;
using Content.Shared._RedStar.Beekeeping.Components;
using Content.Shared._RedStar.Beekeeping.Prototypes;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Destructible;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests._RedStar.Beekeeping;

/// <summary>
/// The swarm quirks add up to a clean 100 and the ones that take a share of the colony stay sensible.
/// </summary>
[TestOf(typeof(SwarmModifierPrototype))]
public sealed class SwarmModifierTest : GameTest
{
    private static readonly ProtoId<SwarmModifierPrototype> Plain = "Plain";

    [Test]
    public async Task TaintedHiveAddsAReagentToItsCombs()
    {
        var server = Server;
        var map = await Pair.CreateTestMap();
        var solutions = server.System<SharedSolutionContainerSystem>();
        var destructible = server.System<SharedDestructibleSystem>();

        await server.WaitAssertion(() =>
        {
            var entities = server.EntMan;
            var hive = entities.SpawnAttachedTo("WildBeehive", map.GridCoords);
            var comp = entities.GetComponent<BeehiveComponent>(hive);
            comp.SwarmModifier = "TaintedHive";
            comp.WildCombs = 1;
            destructible.DestroyEntity(hive);

            var comb = entities.AllEntities<MetaDataComponent>()
                .Single(entity => entity.Comp.EntityPrototype?.ID == "FoodHoneycomb");
            Assert.That(solutions.TryGetSolution(comb.Owner, "food", out _, out var food), Is.True);
            var honey = food!.GetTotalPrototypeQuantity("Honey");
            var vitamin = food.GetTotalPrototypeQuantity("Vitamin");
            Assert.That((food.Volume - honey - vitamin).Float(), Is.GreaterThan(0f));
        });
    }

    [Test]
    public async Task WeightsAndShares()
    {
        var proto = Server.ProtoMan;
        await Server.WaitAssertion(() =>
        {
            var modifiers = proto.EnumeratePrototypes<SwarmModifierPrototype>().ToList();
            Assert.That(modifiers.Sum(m => m.Weight), Is.EqualTo(100f).Within(0.01f));
            Assert.That(modifiers.Select(m => m.ColonyShare), Has.All.InRange(0.1f, 0.9f));
            Assert.That(proto.Index(Plain).Weight, Is.EqualTo(modifiers.Max(m => m.Weight)), "A plain swarm is the most common.");
        });
    }
}
