#nullable enable
using System.Linq;
using Content.IntegrationTests.Fixtures;
using Content.Server._RedStar.Beekeeping.Components;
using Content.Server.Atmos.EntitySystems;
using Content.Shared._RedStar.Beekeeping.Components;
using Content.Shared.Atmos;
using Content.Shared.Containers.ItemSlots;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;

namespace Content.IntegrationTests.Tests._RedStar.Beekeeping;

/// <summary>
/// A hive with a queen and no frames is no home: after a while the queen leaves it with all her bees as a swarm.
/// </summary>
[TestOf(typeof(BeehiveComponent))]
public sealed class HiveAbandonTest : GameTest
{
    private static readonly TimeSpan ShortAbandon = TimeSpan.FromSeconds(6);

    private EntityUid _hive;
    private EntityUid _queen;
    private TestMapData _map = default!;

    private async Task Build(int bees)
    {
        var server = Server;
        _map = await Pair.CreateTestMap();
        await server.WaitPost(() =>
        {
            var air = new GasMixture(Atmospherics.CellVolume) { Temperature = Atmospherics.T20C };
            air.AdjustMoles(Gas.Oxygen, Atmospherics.OxygenMolesStandard);
            air.AdjustMoles(Gas.Nitrogen, Atmospherics.NitrogenMolesStandard);
            server.System<AtmosphereSystem>().SetMapAtmosphere(_map.MapUid, false, air);

            _hive = server.EntMan.SpawnEntity("Beehive", _map.GridCoords);
            var comp = server.EntMan.GetComponent<BeehiveComponent>(_hive);
            Assert.That(comp.AbandonTime, Is.EqualTo(TimeSpan.FromMinutes(3)));
            comp.AbandonTime = ShortAbandon;

            _queen = server.EntMan.SpawnEntity("BeeQueen", _map.GridCoords);
            server.EntMan.GetComponent<BeeQueenComponent>(_queen).Traits.Clear();
            Assert.That(server.System<ItemSlotsSystem>().TryInsert(_hive, comp.QueenSlot, _queen, null));
            comp.Bees = bees;
            comp.NextScoutTrip = TimeSpan.MaxValue;
        });
        await RunTicksSync(5);
    }

    private bool QueenHome()
    {
        var comp = Server.EntMan.GetComponent<BeehiveComponent>(_hive);
        return Server.System<ItemSlotsSystem>().GetItemOrNull(_hive, comp.QueenSlot) == _queen;
    }

    private bool QueenInSwarm()
    {
        return Server.System<SharedContainerSystem>().TryGetContainingContainer(_queen, out var container) &&
               Server.EntMan.HasComponent<BeeSwarmComponent>(container.Owner);
    }

    /// <summary>
    /// Правило: улей с маткой без рамок через 3 минуты брошен, матка и все пчёлы улетают роем.
    /// </summary>
    [Test]
    public async Task FramelessHiveIsAbandoned()
    {
        await Build(4);

        await RunSeconds((float) ShortAbandon.TotalSeconds / 2);
        await Server.WaitAssertion(() => Assert.That(QueenHome(), "left before the time ran out"));

        await RunSeconds((float) ShortAbandon.TotalSeconds / 2 + 2);
        await Server.WaitAssertion(() =>
        {
            var comp = Server.EntMan.GetComponent<BeehiveComponent>(_hive);
            Assert.Multiple(() =>
            {
                Assert.That(QueenHome(), Is.False, "the queen stayed in a hive without frames");
                Assert.That(QueenInSwarm(), "the queen did not leave in a swarm");
                Assert.That(comp.Bees, Is.Zero, "bees stayed behind");
                var swarm = Server.EntMan.EntityQuery<BeeSwarmComponent>().Single();
                Assert.That(swarm.Bees, Is.EqualTo(4));
            });
        });
    }

    /// <summary>
    /// Правило: если за 3 минуты вставили рамку, отсчёт сбрасывается и колония остаётся.
    /// </summary>
    [Test]
    public async Task FrameInTimeKeepsTheColony()
    {
        await Build(4);
        await RunSeconds((float) ShortAbandon.TotalSeconds / 2);
        await Server.WaitPost(() =>
        {
            var comp = Server.EntMan.GetComponent<BeehiveComponent>(_hive);
            var frame = Server.EntMan.SpawnEntity("BeeFrame", _map.GridCoords);
            Assert.That(Server.System<ItemSlotsSystem>().TryInsert(_hive, comp.FrameSlots[0], frame, null));
        });

        await RunSeconds((float) ShortAbandon.TotalSeconds * 2);
        await Server.WaitAssertion(() =>
        {
            Assert.That(QueenHome(), "the colony left a hive with a frame");
            Assert.That(Server.EntMan.EntityQuery<BeeSwarmComponent>(), Is.Empty);
        });
    }

    /// <summary>
    /// Правило: матка одна в улье без рамок тоже улетает искать новый дом.
    /// </summary>
    [Test]
    public async Task LoneQueenLeavesToo()
    {
        await Build(0);
        await RunSeconds((float) ShortAbandon.TotalSeconds + 2);
        await Server.WaitAssertion(() =>
        {
            Assert.That(QueenHome(), Is.False, "a lone queen stayed in a hive without frames");
            Assert.That(QueenInSwarm(), "the lone queen did not fly off");
        });
    }
}
