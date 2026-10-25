using Content.Shared._Trauma.Augments;
using Content.Shared.Actions.Components;
using Content.Shared.Item.ItemToggle;
using Content.Shared.Power.EntitySystems;
using Content.Shared.Body;
using Content.Shared.Verbs;
using System.Linq;
using Content.Shared.PowerCell.Components;
using Content.Shared.Storage.EntitySystems;
using Content.Shared.Weapons.Melee;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Localization;

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

    [Test]
    public async Task StrengthAugmentCanBeInstalledAndLosesPower()
    {
        var (surgeon, patient) = await SetupPatient();
        EntityUid torso = default;
        EntityUid arm = default;
        EntityUid slot = default;
        EntityUid strength = default;
        EntityUid battery = default;
        await Server.WaitAssertion(() =>
        {
            torso = _body.GetOrgan(patient, Torso)!.Value;
            arm = _body.GetOrgan(patient, Arm)!.Value;
            slot = SSpawn("AugmentPowerCellSlot");
            strength = SSpawn("AugmentStrength");
            battery = SSpawn("PowerCellHigh");
        });
        await Operate(surgeon, patient, torso, "SurgeryOpenPowerCellSlot");
        await Operate(surgeon, patient, torso, "SurgeryInsertPowerCellSlot", slot);
        await Server.WaitAssertion(() => InsertAugmentCell(slot, battery));
        await Operate(surgeon, patient, arm, "SurgeryOpenArmAugment");
        await Operate(surgeon, patient, arm, "SurgeryInsertArmAugment", strength);
        await Server.WaitAssertion(() =>
        {
            Assert.That(_parts.GetOrgan(arm, ArmAugment), Is.EqualTo(strength));
            var melee = Server.System<SharedMeleeWeaponSystem>();
            var before = melee.GetDamage(patient, patient);
            Assert.That(Server.System<ItemToggleSystem>().TryActivate(strength, user: patient), Is.True);
            Assert.That(melee.GetDamage(patient, patient).GetTotal(), Is.EqualTo((before * 1.25f).GetTotal()));
            Assert.That(SComp<PowerCellDrawComponent>(slot).DrawRate, Is.EqualTo(30f));
        });
        await RunSeconds(2);
        await Server.WaitAssertion(() =>
        {
            var charge = Server.System<SharedBatterySystem>().GetCharge(battery);
            Assert.That(charge.Charge, Is.LessThan(charge.MaxCharge));
            Server.System<SharedBatterySystem>().SetCharge(battery, 0);
        });
        await RunSeconds(3);
        await Server.WaitAssertion(() =>
        {
            Assert.That(Server.System<ItemToggleSystem>().IsActivated(strength), Is.False);
            Server.System<SharedBatterySystem>().SetCharge(battery, 1000);
            Assert.That(Server.System<ItemToggleSystem>().TryActivate(strength, user: patient), Is.True);
            var action = SComp<AugmentActionComponent>(strength).ActionEntity;
            Assert.That(action, Is.Not.Null);
            Assert.That(SComp<ActionComponent>(action!.Value).AttachedEntity, Is.EqualTo(patient));
        });
    }

    [Test]
    public async Task ToolPanelRetractsItsToolWhenPowerFails()
    {
        var (_, patient) = await SetupPatient();
        EntityUid panel = default;
        EntityUid tool = default;
        EntityUid battery = default;
        await Server.WaitAssertion(() =>
        {
            var torso = _body.GetOrgan(patient, Torso)!.Value;
            var arm = _body.GetOrgan(patient, Arm)!.Value;
            var slot = SSpawn("AugmentPowerCellSlot");
            panel = SSpawn("AugmentToolsPanel");
            tool = SSpawn("Wrench");
            battery = SSpawn("PowerCellHigh");
            Assert.That(_parts.TryAddSlot(torso, AugmentCell), Is.True);
            Assert.That(_parts.InsertOrgan(torso, slot), Is.True);
            InsertAugmentCell(slot, battery);
            Assert.That(_parts.TryAddSlot(arm, ArmAugment), Is.True);
            Assert.That(_parts.InsertOrgan(arm, panel), Is.True);
            Assert.That(Server.System<SharedStorageSystem>().Insert(panel, tool, out _, null), Is.True);
            Server.System<AugmentToolPanelSystem>().SwitchTool((panel, SComp<AugmentToolPanelComponent>(panel)), tool, patient);
            Assert.That(_hands.IsHolding(patient, tool), Is.True);
            Assert.That(SComp<PowerCellDrawComponent>(slot).DrawRate, Is.EqualTo(6f));
            Assert.That(_hands.TryDrop(patient, tool), Is.False);
            Server.System<SharedBatterySystem>().SetCharge(battery, 0);
        });
        await RunSeconds(3);
        await Server.WaitAssertion(() =>
        {
            Assert.That(_hands.IsHolding(patient, tool), Is.False);
            var containers = Server.System<SharedContainerSystem>();
            Assert.That(containers.TryGetContainingContainer(tool, out var container), Is.True);
            Assert.That(container!.Owner, Is.EqualTo(panel));
        });
    }
    private static readonly ProtoId<OrganCategoryPrototype> AugmentCharger = "AugmentCharger";

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
