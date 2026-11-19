#nullable enable
using System.Linq;
using Content.IntegrationTests.Fixtures;
using Content.Server._RedStar.Beekeeping.Components;
using Content.Server._RedStar.Beekeeping.Systems;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Inventory;
using Content.Shared.NPC.Systems;
using Content.Shared._RedStar.Beekeeping.Components;
using Content.Shared._RedStar.Beekeeping.Data;
using Robust.Shared.GameObjects;

namespace Content.IntegrationTests.Tests._RedStar.Beekeeping;

/// <summary>
/// A hit lets angry bees out of the colony (one per hit plus one per damage step, the scout first), a killed bee is
/// lost to the colony, and a broken hive lets everyone out with the queen defending it.
/// </summary>
[TestOf(typeof(BeehiveComponent))]
public sealed class AngryBeehiveTest : GameTest
{
    [Test]
    public async Task HitsAndBreaking()
    {
        var server = Server;
        var map = await Pair.CreateTestMap();
        var slots = server.System<ItemSlotsSystem>();
        var damageable = server.System<DamageableSystem>();
        EntityUid hive = default;

        await server.WaitPost(() =>
        {
            hive = server.EntMan.SpawnEntity("Beehive", map.GridCoords);
            var comp = server.EntMan.GetComponent<BeehiveComponent>(hive);
            Assert.That(slots.TryInsert(hive, comp.FrameSlots[0], server.EntMan.SpawnEntity("BeeFrame", map.GridCoords), null));
            Assert.That(slots.TryInsert(hive, comp.QueenSlot, server.EntMan.SpawnEntity("BeeQueen", map.GridCoords), null));
            comp.Bees = 5;

            // 25 damage: one bee for the hit and two for two full steps of 10.
            damageable.TryChangeDamage(hive, new DamageSpecifier { DamageDict = { ["Blunt"] = 25 } });

            Assert.Multiple(() =>
            {
                Assert.That(comp.AngryOut, Is.EqualTo(3));
                Assert.That(comp.Bees, Is.EqualTo(2));
                Assert.That(comp.AngryScout, Is.Not.Null);
                Assert.That(comp.ScoutState, Is.EqualTo(BeeScoutState.Outside));
            });
        });
        await RunTicksSync(5);

        await server.WaitPost(() =>
        {
            var comp = server.EntMan.GetComponent<BeehiveComponent>(hive);
            EntityUid? worker = null;
            var bees = server.EntMan.EntityQueryEnumerator<BeehiveAngryBeeComponent>();
            while (worker == null && bees.MoveNext(out var uid, out var angry))
            {
                if (!angry.Scout)
                    worker = uid;
            }

            server.EntMan.DeleteEntity(worker);

            Assert.Multiple(() =>
            {
                Assert.That(comp.AngryOut, Is.EqualTo(2));
                Assert.That(comp.OutBees, Is.EqualTo(2));
                Assert.That(comp.Bees, Is.EqualTo(2));
            });

            damageable.TryChangeDamage(hive, new DamageSpecifier { DamageDict = { ["Blunt"] = 200 } });
        });
        await RunTicksSync(5);

        await server.WaitAssertion(() =>
        {
            Assert.That(server.EntMan.Deleted(hive));
            Assert.That(server.EntMan.EntityQuery<BeehiveAngryQueenComponent>().Count(), Is.EqualTo(1));
            // The queen item rides inside the angry queen; it was not lost with the hive.
            Assert.That(server.EntMan.EntityQuery<BeeQueenComponent>().Count(), Is.EqualTo(1));
            // The two bees left at home flew out too.
            Assert.That(server.EntMan.EntityQuery<BeehiveAngryBeeComponent>().Count(), Is.EqualTo(4));
        });
    }

    /// <summary>
    /// Angry bees do not go for a person in a sealed suit and helmet, and go for them again once the helmet is off.
    /// </summary>
    [Test]
    public async Task SealedPeopleAreLeftAlone()
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
            var suit = server.EntMan.SpawnEntity("ClothingOuterSuitEmergency", map.GridCoords);
            var helmet = server.EntMan.SpawnEntity("ClothingHeadHelmetEVA", map.GridCoords);
            Assert.That(inventory.TryEquip(human, suit, "outerClothing", force: true));
            Assert.That(inventory.TryEquip(human, helmet, "head", force: true));
        });

        await RunTicksSync(60);

        await server.WaitAssertion(() =>
        {
            Assert.That(faction.IsIgnored(bee, human), "a sealed person is ignored");
            Assert.That(faction.GetHostiles(bee), Does.Not.Contain(human));
            Assert.That(inventory.TryUnequip(human, "head", out _, force: true));
        });

        await RunTicksSync(60);

        await server.WaitAssertion(() =>
        {
            Assert.That(faction.IsIgnored(bee, human), Is.False, "no longer ignored without the helmet");
        });
    }

    /// <summary>
    /// When its time is up an angry bee flies back into the hive by itself, and the colony has it back.
    /// </summary>
    [Test]
    public async Task AngryBeeFliesHome()
    {
        var server = Server;
        var map = await Pair.CreateTestMap();
        var slots = server.System<ItemSlotsSystem>();
        EntityUid hive = default;

        await server.WaitPost(() =>
        {
            var mapSystem = server.System<SharedMapSystem>();
            for (var x = -3; x <= 3; x++)
            {
                for (var y = -3; y <= 3; y++)
                    mapSystem.SetTile(map.Grid.Owner, map.Grid.Comp, new Robust.Shared.Maths.Vector2i(x, y), map.Tile.Tile);
            }

            hive = server.EntMan.SpawnEntity("Beehive", map.GridCoords);
            var comp = server.EntMan.GetComponent<BeehiveComponent>(hive);
            Assert.That(slots.TryInsert(hive, comp.FrameSlots[0], server.EntMan.SpawnEntity("BeeFrame", map.GridCoords), null));
            Assert.That(slots.TryInsert(hive, comp.QueenSlot, server.EntMan.SpawnEntity("BeeQueen", map.GridCoords), null));
            comp.Bees = 5;
            // As in the test above: three bees fly out, the scout among them.
            server.System<DamageableSystem>().TryChangeDamage(hive, new DamageSpecifier { DamageDict = { ["Blunt"] = 25 } });
            Assert.That(comp.AngryOut, Is.EqualTo(3));
            Assert.That(comp.Bees, Is.EqualTo(2));

            // Time is up at once.
            var bees = server.EntMan.EntityQueryEnumerator<BeehiveAngryBeeComponent>();
            while (bees.MoveNext(out _, out var angry))
                angry.ReturnAt = server.Timing.CurTime;
        });

        await RunSeconds(15);
        await server.WaitAssertion(() =>
        {
            var comp = server.EntMan.GetComponent<BeehiveComponent>(hive);
            Assert.That(comp.AngryOut, Is.Zero, "no bee is out any more");
            Assert.That(comp.Bees, Is.EqualTo(5), "they flew home");
            Assert.That(comp.ScoutState, Is.EqualTo(BeeScoutState.Home), "the scout is home too");
        });
    }

    /// <summary>
    /// In water vapor the colony keeps calm: a hit lets no angry bees out.
    /// </summary>
    [Test]
    public async Task VaporKeepsTheHiveCalm()
    {
        var server = Server;
        var map = await Pair.CreateTestMap();
        var slots = server.System<ItemSlotsSystem>();
        EntityUid hive = default;

        await server.WaitPost(() =>
        {
            var air = new Content.Shared.Atmos.GasMixture(Content.Shared.Atmos.Atmospherics.CellVolume)
            {
                Temperature = Content.Shared.Atmos.Atmospherics.T20C,
            };
            air.AdjustMoles(Content.Shared.Atmos.Gas.Oxygen, 21f);
            air.AdjustMoles(Content.Shared.Atmos.Gas.Nitrogen, 79f);
            air.AdjustMoles(Content.Shared.Atmos.Gas.WaterVapor, 5f);
            server.System<Content.Server.Atmos.EntitySystems.AtmosphereSystem>().SetMapAtmosphere(map.MapUid, false, air);

            hive = server.EntMan.SpawnEntity("Beehive", map.GridCoords);
            var comp = server.EntMan.GetComponent<BeehiveComponent>(hive);
            Assert.That(slots.TryInsert(hive, comp.FrameSlots[0], server.EntMan.SpawnEntity("BeeFrame", map.GridCoords), null));
            Assert.That(slots.TryInsert(hive, comp.QueenSlot, server.EntMan.SpawnEntity("BeeQueen", map.GridCoords), null));
            comp.Bees = 5;
        });
        await RunTicksSync(5);

        await server.WaitAssertion(() =>
        {
            var comp = server.EntMan.GetComponent<BeehiveComponent>(hive);
            Assert.That(server.System<BeehiveSystem>().InCalmingVapor(hive, comp), "the vapor is around the hive");
            server.System<DamageableSystem>().TryChangeDamage(hive, new DamageSpecifier { DamageDict = { ["Blunt"] = 25 } });
            Assert.That(comp.AngryOut, Is.Zero, "no angry bee came out");
            Assert.That(comp.Bees, Is.EqualTo(5));
        });
    }

    /// <summary>
    /// Someone hits a forager: a couple of angry bees come out of its hive after them.
    /// </summary>
    [Test]
    public async Task HittingAForagerBringsDefenders()
    {
        var server = Server;
        var map = await Pair.CreateTestMap();
        var slots = server.System<ItemSlotsSystem>();

        await server.WaitAssertion(() =>
        {
            var hive = server.EntMan.SpawnEntity("Beehive", map.GridCoords);
            var comp = server.EntMan.GetComponent<BeehiveComponent>(hive);
            Assert.That(slots.TryInsert(hive, comp.FrameSlots[0], server.EntMan.SpawnEntity("BeeFrame", map.GridCoords), null));
            Assert.That(slots.TryInsert(hive, comp.QueenSlot, server.EntMan.SpawnEntity("BeeQueen", map.GridCoords), null));
            comp.Bees = 5;
            var tray = server.EntMan.SpawnEntity("HydroponicsTrayEmpty", map.GridCoords);
            var bee = server.System<BeeErrandSystem>().TrySpawnForager((hive, comp), server.EntMan.GetComponent<TransformComponent>(hive), tray);
            Assert.That(bee, Is.Not.Null);

            var attacker = server.EntMan.SpawnEntity("MobHuman", map.GridCoords);
            server.System<DamageableSystem>().TryChangeDamage(bee!.Value, new DamageSpecifier { DamageDict = { ["Blunt"] = 1 } }, origin: attacker);
            Assert.That(comp.AngryOut, Is.EqualTo(2), "two bees came out to defend it");
        });
    }
}
