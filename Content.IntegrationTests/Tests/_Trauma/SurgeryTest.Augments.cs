using System.Linq;
using Content.Shared._Trauma.Augments;
using Content.Shared.Actions.Components;
using Content.Shared.Body;
using Content.Shared.Item.ItemToggle;
using Content.Shared.Power.EntitySystems;
using Content.Shared.PowerCell.Components;
using Content.Shared.Storage.EntitySystems;
using Content.Shared.Verbs;
using Content.Shared.Weapons.Melee;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests._Trauma;

public sealed partial class SurgeryTest
{
    private static readonly ProtoId<OrganCategoryPrototype> AugmentCell = "AugmentPowerCell";
    private static readonly ProtoId<OrganCategoryPrototype> ArmAugment = "ArmAugment";

    private void InsertAugmentCell(EntityUid slot, EntityUid battery)
    {
        var containers = Server.System<SharedContainerSystem>();
        Assert.That(containers.TryGetContainer(slot, "cell_slot", out var container), Is.True);
        Assert.That(containers.Insert(battery, container!), Is.True);
    }

    private static readonly ProtoId<OrganCategoryPrototype> AugmentCharger = "AugmentCharger";

    private static readonly ProtoId<OrganCategoryPrototype> CyberneticEyes = "Eyes";

    [Test]
    public async Task CyberneticEffectsFollowOrganState()
    {
        var (_, patient) = await SetupPatient();
        await Server.WaitAssertion(() =>
        {
            var head = _body.GetOrgan(patient, Head)!.Value;
            var oldEyes = _parts.GetOrgan(head, CyberneticEyes)!.Value;
            Assert.That(_parts.RemoveOrgan(head, oldEyes), Is.True);
            var eyes = SSpawn("OrganCyberneticEyesMedical");
            Assert.That(_parts.InsertOrgan(head, eyes), Is.True);
            Assert.That(SEntMan.HasComponent<Content.Shared.Flash.Components.FlashImmunityComponent>(patient), Is.True);

            Assert.That(_body.DisableOrgan(eyes), Is.True);
            Assert.That(SEntMan.HasComponent<Content.Shared.Flash.Components.FlashImmunityComponent>(patient), Is.False);
            Assert.That(_body.EnableOrgan(eyes), Is.True);
            Assert.That(SEntMan.HasComponent<Content.Shared.Flash.Components.FlashImmunityComponent>(patient), Is.True);

            Assert.That(_body.DisableOrgan(eyes), Is.True);
            Assert.That(_parts.RemoveOrgan(head, eyes), Is.True);
            Assert.That(_body.EnableOrgan(eyes), Is.False);
            Assert.That(SEntMan.HasComponent<Content.Shared.Flash.Components.FlashImmunityComponent>(patient), Is.False);
        });
    }

    [Test]
    public async Task ApcChargerCanChargeAnInstalledCell()
    {
        var (_, patient) = await SetupPatient();
        EntityUid battery = default;
        await Server.WaitAssertion(() =>
        {
            var torso = _body.GetOrgan(patient, Torso)!.Value;
            var slot = SSpawn("AugmentPowerCellSlot");
            battery = SSpawn("PowerCellHigh");
            Assert.That(_parts.TryAddSlot(torso, AugmentCell), Is.True);
            Assert.That(_parts.InsertOrgan(torso, slot), Is.True);
            InsertAugmentCell(slot, battery);
            Assert.That(_parts.TryAddSlot(torso, AugmentCharger), Is.True);
            Assert.That(_parts.InsertOrgan(torso, SSpawn("AugmentRechargerAPC")), Is.True);
            Server.System<SharedBatterySystem>().SetCharge(battery, 100);
            var apc = SSpawnAtPosition("APCBasic", TestMap!.GridCoords);
            var verbs = Server.System<SharedVerbSystem>().GetLocalVerbs(apc, patient, typeof(AlternativeVerb));
            var charge = verbs.SingleOrDefault(v => v.Text == Loc.GetString("battery-drinker-verb-drink"));
            Assert.That(charge?.Act, Is.Not.Null);
            charge!.Act!();
        });
        await RunSeconds(3);
        await Server.WaitAssertion(() => Assert.That(Server.System<SharedBatterySystem>().GetCharge(battery).Charge, Is.GreaterThan(100)));
    }

}
