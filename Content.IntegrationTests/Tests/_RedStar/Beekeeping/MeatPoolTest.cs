#nullable enable
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Content.IntegrationTests.Fixtures;
using Content.Server._RedStar.Beekeeping.Systems;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Destructible;
using Content.Shared.Stacks;
using Content.Shared._RedStar.Beekeeping.Components;
using Content.Shared._RedStar.Beekeeping.Data;
using Content.Shared._RedStar.Beekeeping.Prototypes;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests._RedStar.Beekeeping;

/// <summary>
/// What falls out of a broken meat hive: how many pieces, which kinds by weight, and what the combs carry.
/// </summary>
[TestOf(typeof(BeeMeatPrototype))]
public sealed class MeatPoolTest : GameTest
{
    private static readonly ProtoId<BeeMeatPrototype> PlainMeat = "Plain";
    private static readonly ProtoId<BeeMeatPrototype> SlimeMeat = "Slime";
    private static readonly ProtoId<BeeMeatPrototype> XenoMeat = "Xeno";
    private static readonly ProtoId<BeeMeatPrototype> MixedMeat = "Mixed";
    private static readonly ProtoId<ReagentPrototype> BloodReagent = "Blood";

    private static List<ProtoId<ReagentPrototype>> Bloods(params string[] ids)
    {
        return ids.Select(id => new ProtoId<ReagentPrototype>(id)).ToList();
    }

    [Test]
    public async Task WeightsFollowTheBlood()
    {
        var proto = Server.ProtoMan;
        await Server.WaitAssertion(() =>
        {
            var plain = proto.Index(PlainMeat);
            var slime = proto.Index(SlimeMeat);
            var xeno = proto.Index(XenoMeat);
            var mixed = proto.Index(MixedMeat);

            Assert.Multiple(() =>
            {
                // One human: plain meat nearly always, and no rare kinds at all.
                var human = Bloods("Blood");
                Assert.That(plain.WeightFor(human), Is.GreaterThan(slime.WeightFor(human) * 10));
                Assert.That(xeno.WeightFor(human), Is.Zero);
                Assert.That(mixed.WeightFor(human), Is.Zero);

                // Slime blood makes slime meat far likelier.
                Assert.That(slime.WeightFor(Bloods("Blood", "Slime")), Is.GreaterThan(slime.WeightFor(human) * 5));

                // Several kinds of blood open the rare kinds, and more kinds make them likelier.
                var three = Bloods("Blood", "Slime", "InsectBlood");
                var five = Bloods("Blood", "Slime", "InsectBlood", "CopperBlood", "AmmoniaBlood");
                Assert.That(xeno.WeightFor(three), Is.GreaterThan(0f));
                Assert.That(xeno.WeightFor(five), Is.GreaterThan(xeno.WeightFor(three)));
                Assert.That(mixed.WeightFor(three), Is.Zero);
                Assert.That(mixed.WeightFor(five), Is.GreaterThan(0f));
            });
        });
    }

    [Test]
    public async Task BrokenMeatHiveDropsMeatAndBloodyCombs()
    {
        var server = Server;
        var map = await Pair.CreateTestMap();
        var hive = default(EntityUid);

        await server.WaitPost(() =>
        {
            hive = server.EntMan.SpawnEntity("WildBeehive", new EntityCoordinates(map.Grid.Owner, new Vector2(0.5f, 0.5f)));
            var comp = server.EntMan.GetComponent<BeehiveComponent>(hive);
            comp.Diet = BeehiveDiet.Meat;
            comp.WildCombs = 3;
            server.System<MeatBeehiveSystem>().MeatOf(hive).Meat = 6;
            server.System<MeatBeehiveSystem>().MeatOf(hive).BloodTypes.Add("Blood");
            Assert.That(MeatBeehiveSystem.MeatDrops(server.System<MeatBeehiveSystem>().MeatOf(hive)), Is.EqualTo(3), "one and one more for every three bites on the dead");
            server.System<SharedDestructibleSystem>().DestroyEntity(hive);
        });
        await RunTicksSync(5);

        await server.WaitAssertion(() =>
        {
            var meat = server.EntMan.AllEntities<MetaDataComponent>()
                .Count(e => e.Comp.EntityPrototype?.ID.StartsWith("FoodMeat") == true &&
                            e.Comp.EntityPrototype.ID != "FoodHoneycomb");
            Assert.That(meat, Is.EqualTo(3));

            var solutions = server.System<SharedSolutionContainerSystem>();
            var combs = server.EntMan.AllEntities<StackComponent>().Where(e => e.Comp.StackTypeId == "Honeycomb").ToList();
            Assert.That(combs, Has.Count.EqualTo(1));
            Assert.That(solutions.TryGetSolution(combs[0].Owner, "food", out _, out var food));
            Assert.That(food!.GetTotalPrototypeQuantity("Protein").Float(), Is.GreaterThan(0f), "the healing reagent");
        });
    }

    [Test]
    public async Task AnalyzerShowsMeatAndChemicals()
    {
        var server = Server;
        var map = await Pair.CreateTestMap();
        var analyzer = server.System<ApiaryAnalyzerSystem>();
        var hive = default(EntityUid);

        await server.WaitPost(() =>
        {
            hive = server.EntMan.SpawnEntity("WildBeehive", new EntityCoordinates(map.Grid.Owner, new Vector2(0.5f, 0.5f)));
            var comp = server.EntMan.GetComponent<BeehiveComponent>(hive);
            comp.Diet = BeehiveDiet.Meat;
            comp.WildCombs = 2;
            server.System<MeatBeehiveSystem>().MeatOf(hive).Meat = 4;
            server.System<MeatBeehiveSystem>().MeatOf(hive).BloodTypes.Add("Blood");
            server.System<MeatBeehiveSystem>().MeatOf(hive).Blood.AddReagent(BloodReagent, 3);
        });

        await server.WaitAssertion(() =>
        {
            var state = analyzer.BuildState(hive)!;
            Assert.That(state.Diet, Is.EqualTo(BeehiveDiet.Meat));
            Assert.That(state.MeatDrops, Is.EqualTo(2));
            Assert.That(state.MeatChances.Sum(c => c.Percent), Is.EqualTo(100f).Within(0.1f));
            Assert.That(state.Chemicals.Select(c => c.Reagent.Id), Is.EquivalentTo(new[] { "Blood", "Protein" }));
            Assert.That(state.Chemicals.First(c => c.Reagent == "Protein").Amount, Is.EqualTo(4f), "two combs, 2u each");
        });
    }
}
