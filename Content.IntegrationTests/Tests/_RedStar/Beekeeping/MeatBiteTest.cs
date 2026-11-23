#nullable enable
using System.Linq;
using System.Numerics;
using Content.IntegrationTests.Fixtures;
using Content.Server._RedStar.Beekeeping.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.Chemistry.Reagent;
using Content.Shared._RedStar.Beekeeping.Components;
using Content.Shared._RedStar.Beekeeping.Data;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Maths;

namespace Content.IntegrationTests.Tests._RedStar.Beekeeping;

/// <summary>
/// A meat hive uses creatures with blood as its sources and takes real blood from them; bites on the same creature
/// give less and less.
/// </summary>
[TestOf(typeof(BeehiveSystem))]
public sealed class MeatBiteTest : GameTest
{
    [Test]
    public void BiteFactors()
    {
        Assert.Multiple(() =>
        {
            Assert.That(MeatBeehiveSystem.BiteFactor(0, 0.25f), Is.EqualTo(1f));
            Assert.That(MeatBeehiveSystem.BiteFactor(1, 0.25f), Is.EqualTo(0.5f));
            Assert.That(MeatBeehiveSystem.BiteFactor(2, 0.25f), Is.EqualTo(0.25f));
            Assert.That(MeatBeehiveSystem.BiteFactor(7, 0.25f), Is.EqualTo(0.25f));
        });
    }

    [Test]
    public async Task CreaturesAsSources()
    {
        var server = Server;
        var map = await Pair.CreateTestMap();
        var hives = server.System<BeehiveSystem>();
        var hive = default(EntityUid);
        var human = default(EntityUid);
        var mouse1 = default(EntityUid);
        var mouse2 = default(EntityUid);

        EntityCoordinates At(float x) => new(map.Grid.Owner, new Vector2(x, 0.5f));

        await server.WaitPost(() =>
        {
            // Floor for the creatures to stand on.
            var mapSys = server.System<SharedMapSystem>();
            var plating = new Tile(server.ResolveDependency<ITileDefinitionManager>()["Plating"].TileId);
            for (var x = 0; x <= 8; x++)
                mapSys.SetTile(map.Grid.Owner, map.Grid.Comp, new Vector2i(x, 0), plating);

            hive = server.EntMan.SpawnEntity("Beehive", At(0.5f));
            server.EntMan.GetComponent<BeehiveComponent>(hive).Diet = BeehiveDiet.Meat;
            human = server.EntMan.SpawnEntity("MobHuman", At(3.5f));
            mouse1 = server.EntMan.SpawnEntity("MobMouse", At(5.5f));
        });
        await RunTicksSync(5);

        await server.WaitAssertion(() =>
        {
            var comp = server.EntMan.GetComponent<BeehiveComponent>(hive);
            var now = server.Timing.CurTime;
            var humanSource = new BeehiveSource(human, now);
            var mouseSource = new BeehiveSource(mouse1, now);
            comp.KnownSources.Add(humanSource);
            comp.KnownSources.Add(mouseSource);

            Assert.That(server.System<BeeScoutSystem>().GetTrayState((hive, comp), humanSource), Is.EqualTo(ApiaryTrayState.Ripe), "a human");
            Assert.That(server.System<BeeScoutSystem>().GetTrayState((hive, comp), mouseSource), Is.EqualTo(ApiaryTrayState.Blind), "one mouse is too few");

            // The bite takes real blood and remembers its kind.
            var taken = server.System<MeatBeehiveSystem>().TakeBlood((hive, comp), human, 1f);
            Assert.That(taken, Is.EqualTo(FixedPoint2.New(1)));
            Assert.That(server.System<MeatBeehiveSystem>().MeatOf(hive).BloodTypes, Does.Contain((ProtoId<ReagentPrototype>) "Blood"));
            Assert.That(server.System<MeatBeehiveSystem>().MeatOf(hive).Blood.Volume, Is.EqualTo(FixedPoint2.New(1)));
        });

        await server.WaitPost(() => mouse2 = server.EntMan.SpawnEntity("MobMouse", At(6.5f)));
        await RunTicksSync(5);

        await server.WaitAssertion(() =>
        {
            var comp = server.EntMan.GetComponent<BeehiveComponent>(hive);
            var mouseSource = comp.KnownSources[1];
            Assert.That(server.System<BeeScoutSystem>().GetTrayState((hive, comp), mouseSource), Is.EqualTo(ApiaryTrayState.Ripe), "two mice");
        });
    }

    /// <summary>
    /// A meat hive has no scout: it picks a target around itself when a worker is free. Small creatures count only in
    /// a group, and a creature bitten a moment ago is left alone.
    /// </summary>
    [Test]
    public async Task HuntPicksTargets()
    {
        var server = Server;
        var map = await Pair.CreateTestMap();
        var hives = server.System<BeehiveSystem>();
        var hive = default(EntityUid);
        var human = default(EntityUid);

        EntityCoordinates At(float x) => new(map.Grid.Owner, new Vector2(x, 0.5f));

        await server.WaitPost(() =>
        {
            var mapSys = server.System<SharedMapSystem>();
            var plating = new Tile(server.ResolveDependency<ITileDefinitionManager>()["Plating"].TileId);
            for (var x = 0; x <= 8; x++)
                mapSys.SetTile(map.Grid.Owner, map.Grid.Comp, new Vector2i(x, 0), plating);

            hive = server.EntMan.SpawnEntity("Beehive", At(0.5f));
            server.EntMan.GetComponent<BeehiveComponent>(hive).Diet = BeehiveDiet.Meat;
            human = server.EntMan.SpawnEntity("MobHuman", At(3.5f));
            server.EntMan.SpawnEntity("MobMouse", At(5.5f));
            server.EntMan.SpawnEntity("MobMouse", At(6.5f));
        });
        await RunTicksSync(5);

        await server.WaitAssertion(() =>
        {
            var comp = server.EntMan.GetComponent<BeehiveComponent>(hive);
            var xform = server.EntMan.GetComponent<TransformComponent>(hive);
            var origin = server.System<SharedTransformSystem>().GetMapCoordinates(hive);
            var now = server.Timing.CurTime;

            Assert.That(server.System<MeatBeehiveSystem>().PickHuntTarget((hive, comp), xform, origin, now, out _), Is.Not.Null);

            // Bitten just now: the human is left alone, the mice are still there.
            comp.KnownSources.Add(new BeehiveSource(human, now) { NextVisit = now + TimeSpan.FromSeconds(10) });
            for (var i = 0; i < 20; i++)
                Assert.That(server.System<MeatBeehiveSystem>().PickHuntTarget((hive, comp), xform, origin, now, out _), Is.Not.EqualTo(human));
        });
    }
}
