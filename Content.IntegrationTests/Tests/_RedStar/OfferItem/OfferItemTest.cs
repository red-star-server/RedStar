using System.Linq;
using Content.IntegrationTests.Tests.Movement;
using Content.Shared._DV;
using Content.Shared._Floof.OfferItem;
using Content.Shared.Alert;
using Content.Shared.Input;
using Content.Shared.Inventory.VirtualItem;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Movement.Pulling.Systems;
using Robust.Shared.GameObjects;
using Robust.Shared.Input;
using OfferItemSystem = Content.Server._Floof.OfferItem.OfferItemSystem;

namespace Content.IntegrationTests.Tests._RedStar.OfferItem;

[TestOf(typeof(SharedOfferItemSystem))]
public sealed class OfferItemTest : MovementTest
{
    [Test]
    public async Task PullOfferTest()
    {
        var pulled = SEntMan.GetEntity(await SpawnTarget("MobHuman"));
        await PressKey(ContentKeyFunctions.TryPullObject);
        await RunTicks(5);
        Assert.That(SEntMan.GetComponent<PullableComponent>(pulled).Puller, Is.EqualTo(SPlayer));

        var receiver = SEntMan.GetEntity(await SpawnTarget("MobHuman"));
        await PressKey(ContentKeyFunctions.OfferItem);
        await PressKey(EngineKeyFunctions.Use);
        await RunTicks(5);
        Assert.That(SEntMan.GetComponent<OfferItemComponent>(receiver).IsInReceiveMode, Is.True);

        await Server.WaitPost(() => Server.System<OfferItemSystem>().Receive(receiver));
        await RunTicks(5);

        Assert.That(SEntMan.GetComponent<PullableComponent>(pulled).Puller, Is.EqualTo(receiver));
        Assert.That(SEntMan.GetComponent<PullerComponent>(SPlayer).Pulling, Is.Null);
        Assert.That(HandSys.EnumerateHeld(SPlayer), Is.Empty);
        Assert.That(HandSys.EnumerateHeld(receiver).Count(), Is.EqualTo(1));
        Assert.That(SEntMan.GetComponent<OfferItemComponent>(receiver).IsInReceiveMode, Is.False);
        Assert.That(Server.System<AlertsSystem>().IsShowingAlert(receiver, "Offer"), Is.False);

        await Server.WaitPost(() => Server.System<PullingSystem>().TryStopPull(pulled, SEntMan.GetComponent<PullableComponent>(pulled)));
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task ItemOfferTest(bool cancelOffer)
    {
        var receiver = SEntMan.GetEntity(await SpawnTarget("MobHuman"));
        var item = SEntMan.GetEntity(await PlaceInHands("Crowbar"));

        await PressKey(ContentKeyFunctions.OfferItem);
        await PressKey(EngineKeyFunctions.Use);
        await RunTicks(5);
        Assert.That(SEntMan.GetComponent<OfferItemComponent>(receiver).IsInReceiveMode, Is.True);

        if (cancelOffer)
            await PressKey(ContentKeyFunctions.OfferItem);
        else
            await Server.WaitPost(() => Server.System<OfferItemSystem>().Receive(receiver));

        await RunTicks(5);
        var expectedHolder = cancelOffer ? SPlayer : receiver;
        Assert.That(HandSys.GetActiveItem(expectedHolder), Is.EqualTo(item));
        Assert.That(SEntMan.GetComponent<OfferItemComponent>(receiver).IsInReceiveMode, Is.False);
        Assert.That(SEntMan.GetComponent<OfferItemComponent>(SPlayer).ReceivingFrom, Is.Null);
        Assert.That(Server.System<AlertsSystem>().IsShowingAlert(receiver, "Offer"), Is.False);
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task CarryOfferTest(bool receiverHasItem)
    {
        var carried = SEntMan.GetEntity(await SpawnTarget("MobHuman"));
        await Server.WaitPost(() =>
        {
            var carriable = SEntMan.GetComponent<CarriableComponent>(carried);
            Server.System<CarryingSystem>().StartCarryDoAfter(SPlayer, (carried, carriable));
        });
        await AwaitDoAfters();

        Assert.That(SEntMan.GetComponent<BeingCarriedComponent>(carried).Carrier, Is.EqualTo(SPlayer));
        Assert.That(HandSys.EnumerateHeld(SPlayer).All(SEntMan.HasComponent<OfferableVirtualItemComponent>), Is.True);

        var receiver = SEntMan.GetEntity(await SpawnTarget("MobHuman"));
        if (receiverHasItem)
        {
            var item = await Spawn("Crowbar");
            await Server.WaitAssertion(() => Assert.That(HandSys.TryPickup(receiver, SEntMan.GetEntity(item)), Is.True));
        }

        await PressKey(ContentKeyFunctions.OfferItem);
        await PressKey(EngineKeyFunctions.Use);
        await RunTicks(5);

        Assert.That(SEntMan.GetComponent<OfferItemComponent>(receiver).IsInReceiveMode, Is.True);
        Assert.That(Server.System<AlertsSystem>().IsShowingAlert(receiver, "Offer"), Is.True);

        await Server.WaitPost(() => Server.System<OfferItemSystem>().Receive(receiver));
        await RunTicks(5);

        var expectedCarrier = receiverHasItem ? SPlayer : receiver;
        Assert.That(SEntMan.GetComponent<BeingCarriedComponent>(carried).Carrier, Is.EqualTo(expectedCarrier));
        Assert.That(SEntMan.GetComponent<TransformComponent>(carried).ParentUid, Is.EqualTo(expectedCarrier));
        Assert.That(HandSys.EnumerateHeld(expectedCarrier).Count(), Is.EqualTo(2));
        Assert.That(HandSys.EnumerateHeld(expectedCarrier).All(SEntMan.HasComponent<VirtualItemComponent>), Is.True);

        if (receiverHasItem)
        {
            Assert.That(SEntMan.HasComponent<CarryingComponent>(receiver), Is.False);
            Assert.That(SEntMan.GetComponent<OfferItemComponent>(receiver).IsInReceiveMode, Is.True);
            await PressKey(ContentKeyFunctions.OfferItem);
        }
        else
        {
            Assert.That(SEntMan.HasComponent<CarryingComponent>(SPlayer), Is.False);
            Assert.That(HandSys.EnumerateHeld(SPlayer), Is.Empty);
        }

        await RunTicks(5);
        Assert.That(SEntMan.GetComponent<OfferItemComponent>(receiver).IsInReceiveMode, Is.False);
        Assert.That(SEntMan.GetComponent<OfferItemComponent>(SPlayer).ReceivingFrom, Is.Null);
        Assert.That(Server.System<AlertsSystem>().IsShowingAlert(receiver, "Offer"), Is.False);
        await Server.WaitPost(() => Server.System<CarryingSystem>().DropCarried(expectedCarrier, carried));
    }
}
