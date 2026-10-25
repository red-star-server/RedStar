using Robust.Shared.Containers;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Robust.Client.UserInterface.RichText;
using System.Linq;
using Content.Shared.Verbs;
using Robust.Shared.Localization;
using Robust.Shared.GameObjects;
using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.Shared._Trauma.Common.Body;
using Content.Shared._Trauma.Common.Targeting;
using Content.Shared._Trauma.Medical;
using Content.Shared._Trauma.Mobs;
using Content.Shared._Trauma.Targeting;
using Content.Shared._Trauma.Traumas;
using Content.Shared._Trauma.Wounds;
using Content.Shared.Body;
using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage.Systems;
using Content.Shared.DoAfter;
using Content.Shared.EntityEffects;
using Content.Shared.EntityEffects.Effects.Damage;
using Content.Shared.FixedPoint;
using Content.Shared.Medical;
using Content.Shared.Inventory;
using Content.Shared.Temperature.Components;
using Content.Shared._Trauma.Common.EntityEffects;
using Content.Shared.Medical.Cryogenics;
using Content.Shared._Trauma.Common.Medical.HealthAnalyzer;
using Content.Shared.Medical.HealthAnalyzer;
using Content.Shared.Medical.Healing;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Stacks;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests._Trauma;

[TestFixture]
public sealed class MedicalTreatmentTest : GameTest
{
    [SidedDependency(Side.Client)] private MarkupTagManager _markup = default!;
    [SidedDependency(Side.Server)] private SharedContainerSystem _containers = default!;
    [SidedDependency(Side.Server)] private SharedSolutionContainerSystem _solutions = default!;
    [SidedDependency(Side.Server)] private IPrototypeManager _prototypes = default!;
    [SidedDependency(Side.Server)] private BodySystem _body = default!;
    [SidedDependency(Side.Server)] private DamageableSystem _damage = default!;
    [SidedDependency(Side.Server)] private WoundSystem _wounds = default!;
    [SidedDependency(Side.Server)] private TraumaSystem _trauma = default!;
    [SidedDependency(Side.Server)] private SharedTargetingSystem _targeting = default!;
    [SidedDependency(Side.Server)] private SharedEntityEffectsSystem _effects = default!;
    [SidedDependency(Side.Server)] private MobStateSystem _mob = default!;
    [SidedDependency(Side.Server)] private MobThresholdSystem _threshold = default!;
    [SidedDependency(Side.Server)] private BloodstreamSystem _blood = default!;
    [SidedDependency(Side.Server)] private HealthAnalyzerSystem _analyzer = default!;
    [SidedDependency(Side.Server)] private HealingSystem _healing = default!;

    [SidedDependency(Side.Server)] private InventorySystem _inventory = default!;

    private static readonly ProtoId<OrganCategoryPrototype> Arm = "ArmLeft";
    private static readonly ProtoId<OrganCategoryPrototype> Head = "Head";
    private static readonly EntProtoId Slash = "Slash";
    private static readonly ProtoId<DamageGroupPrototype> Brute = "Brute";

    private HealingDoAfterEvent FinishTreatment(EntityUid target, EntityUid item)
    {
        var ev = new HealingDoAfterEvent();
        var args = new DoAfterArgs(SEntMan, target, TimeSpan.Zero, ev, target, target: target, used: item);
        ev.DoAfter = new Content.Shared.DoAfter.DoAfter(0, args, TimeSpan.Zero);
        SEntMan.EventBus.RaiseLocalEvent(target, ev);
        return ev;
    }

    [Test]
    [RunOnSide(Side.Client)]
    public void SelfInspectionMarkupHandlerIsRegistered()
    {
        Assert.That(_markup.TryGetMarkupTagHandler("examineborder", null, out _), Is.True);
    }

    [Test]
    [RunOnSide(Side.Server)]
    public void TopicalStartsWithSelectedPartAndSpendsOneUnit()
    {
        var body = SSpawn("MobHuman");
        var arm = _body.GetOrgan(body, Arm)!.Value;
        var head = _body.GetOrgan(body, Head)!.Value;
        _damage.ChangeDamage(body, new DamageSpecifier { DamageDict = { ["Blunt"] = 5 } },
            true, targetPart: TargetBodyPart.LeftArm, canMiss: false);
        _damage.ChangeDamage(body, new DamageSpecifier { DamageDict = { ["Blunt"] = 10 } },
            true, targetPart: TargetBodyPart.Head, canMiss: false);
        _targeting.SetTarget(body, TargetBodyPart.LeftArm);
        var item = SSpawn("Brutepack");
        var before = SComp<StackComponent>(item).Count;
        Assert.That(_healing.IsBodyDamaged((body, SComp<BodyComponent>(body)), body, SComp<HealingComponent>(item)), Is.True);
        var ev = FinishTreatment(body, item);
        Assert.Multiple(() =>
        {
            Assert.That(ev.Handled, Is.True);
            Assert.That(_damage.GetTotalDamage(arm), Is.EqualTo(FixedPoint2.Zero));
            Assert.That(_damage.GetTotalDamage(head), Is.EqualTo((FixedPoint2) 5));
            Assert.That(SComp<StackComponent>(item).Count, Is.EqualTo(before - 1));
        });
    }

    [Test]
    [RunOnSide(Side.Server)]
    public void GauzeStopsWoundBleeding()
    {
        var body = SSpawn("MobHuman");
        var arm = _body.GetOrgan(body, Arm)!.Value;
        _targeting.SetTarget(body, TargetBodyPart.LeftArm);
        Assert.That(_wounds.TryInduceWound(arm, Slash, 5, out _), Is.True);
        var item = SSpawn("Gauze");
        var before = SComp<StackComponent>(item).Count;
        Assert.That(SComp<BloodstreamComponent>(body).BleedAmountFromWounds, Is.GreaterThan(0));
        var ev = FinishTreatment(body, item);
        var update = new BloodstreamUpdateEvent();
        SEntMan.EventBus.RaiseLocalEvent(body, ref update);
        Assert.Multiple(() =>
        {
            Assert.That(ev.Handled, Is.True);
            Assert.That(SComp<BloodstreamComponent>(body).BleedAmountFromWounds, Is.Zero);
            Assert.That(SComp<StackComponent>(item).Count, Is.EqualTo(before - 1));
        });
    }

    [Test]
    [RunOnSide(Side.Server)]
    public void BloodpackRestoresBloodInsteadOfDrainingIt()
    {
        var body = SSpawn("MobHuman");
        _blood.TryModifyBloodLevel(body, -50);
        var before = _blood.GetBloodLevel(body);
        var item = SSpawn("Bloodpack");
        FinishTreatment(body, item);
        Assert.That(_blood.GetBloodLevel(body), Is.GreaterThan(before));
    }

    [Test]
    [RunOnSide(Side.Server)]
    public void MedicineHealsWoundsAcrossTheBody()
    {
        var body = SSpawn("MobHuman");
        var arm = _body.GetOrgan(body, Arm)!.Value;
        var head = _body.GetOrgan(body, Head)!.Value;
        foreach (var part in new[] { TargetBodyPart.LeftArm, TargetBodyPart.Head })
            _damage.ChangeDamage(body, new DamageSpecifier { DamageDict = { ["Blunt"] = 10 } },
                true, targetPart: part, canMiss: false);
        _effects.ApplyEffect(body, new HealthChange { Damage = new DamageSpecifier { DamageDict = { ["Blunt"] = -20 } } });
        Assert.Multiple(() =>
        {
            Assert.That(_damage.GetTotalDamage(arm), Is.LessThan((FixedPoint2) 10));
            Assert.That(_damage.GetTotalDamage(head), Is.LessThan((FixedPoint2) 10));
        });
    }

    [Test]
    [RunOnSide(Side.Server)]
    public void BoneMedicineRestoresBoneIntegrity()
    {
        var body = SSpawn("MobHuman");
        var arm = _body.GetOrgan(body, Arm)!.Value;
        _trauma.DamageBone(arm, 10);
        var before = SComp<BoneComponent>(arm).BoneIntegrity;
        ProtoId<ReagentPrototype> ichor = "Ichor";
        var effects = _prototypes.Index(ichor).Metabolisms!.Metabolisms["Bloodstream"].Effects;
        _effects.TryApplyEffects(body, effects);
        Assert.That(SComp<BoneComponent>(arm).BoneIntegrity, Is.EqualTo(before + (FixedPoint2) 1.5f));
    }

    [Test]
    [RunOnSide(Side.Server)]
    public void BicaridinePrototypeHealsEachInjuredPart()
    {
        var body = SSpawn("MobHuman");
        var arm = _body.GetOrgan(body, Arm)!.Value;
        var head = _body.GetOrgan(body, Head)!.Value;
        foreach (var part in new[] { TargetBodyPart.LeftArm, TargetBodyPart.Head })
            _damage.ChangeDamage(body, new DamageSpecifier { DamageDict = { ["Blunt"] = 10 } },
                true, targetPart: part, canMiss: false);
        ProtoId<ReagentPrototype> bicaridine = "Bicaridine";
        var effects = _prototypes.Index(bicaridine).Metabolisms!.Metabolisms["Bloodstream"].Effects;
        _effects.TryApplyEffects(body, effects);
        Assert.Multiple(() =>
        {
            Assert.That(_damage.GetTotalDamage(arm), Is.EqualTo((FixedPoint2) 9));
            Assert.That(_damage.GetTotalDamage(head), Is.EqualTo((FixedPoint2) 9));
        });
    }

    [TestCase("PillCanisterTramadol", "Tramadol", 7.5f)]
    [TestCase("PillCanisterOxycodone", "Oxycodone", 5f)]
    [TestCase("PillCanisterIbuprofen", "Ibuprofen", 10f)]
    [RunOnSide(Side.Server)]
    public void PrefilledCanisterContainsFifteenCorrectDoses(string prototype, string reagent, float dose)
    {
        var canister = SSpawn(prototype);
        Assert.That(_containers.TryGetContainer(canister, "storagebase", out var storage), Is.True);
        Assert.That(storage!.ContainedEntities, Has.Count.EqualTo(15));
        foreach (var pill in storage.ContainedEntities)
        {
            var quantity = FixedPoint2.Zero;
            foreach (var (_, solution) in _solutions.EnumerateSolutions(pill))
                quantity += solution.Comp.Solution.GetTotalPrototypeQuantity(reagent);
            Assert.That(quantity, Is.EqualTo((FixedPoint2) dose));
        }
    }

    [TestCase(100, MobState.SoftCrit)]
    [TestCase(150, MobState.Critical)]
    [TestCase(200, MobState.Dead)]
    [RunOnSide(Side.Server)]
    public void SourceThresholdsDriveMobState(int amount, MobState expected)
    {
        var body = SSpawn("MobHuman");
        _damage.ChangeDamage(body, new DamageSpecifier { DamageDict = { ["Asphyxiation"] = amount } },
            true, canMiss: false);
        Assert.That(SComp<MobStateComponent>(body).CurrentState, Is.EqualTo(expected));
        Assert.That(SEntMan.HasComponent<SoftCritMobComponent>(body), Is.EqualTo(expected == MobState.SoftCrit));
    }

    [Test]
    [RunOnSide(Side.Server)]
    public void LimbDamageDoesNotCountAsVitalDamage()
    {
        var body = SSpawn("MobHuman");
        _damage.ChangeDamage(body, new DamageSpecifier { DamageDict = { ["Blunt"] = 20 } },
            true, targetPart: TargetBodyPart.LeftArm, canMiss: false);
        Assert.That(_threshold.CheckVitalDamage(body), Is.EqualTo(FixedPoint2.Zero));
        Assert.That(SComp<MobStateComponent>(body).CurrentState, Is.EqualTo(MobState.Alive));
    }

    [Test]
    [RunOnSide(Side.Server)]
    public void SoftCritBreathingIsAssistedByCPR()
    {
        var body = SSpawn("MobHuman");
        _mob.ChangeMobState(body, MobState.SoftCrit);
        var alone = new ModifyInhaledVolumeEvent(1);
        SEntMan.EventBus.RaiseLocalEvent(body, ref alone);
        Assert.That(alone.Volume, Is.EqualTo(0.3f).Within(0.0001f));
        SEntMan.EnsureComponent<ActiveCPRComponent>(body);
        var assisted = new ModifyInhaledVolumeEvent(1);
        SEntMan.EventBus.RaiseLocalEvent(body, ref assisted);
        Assert.That(assisted.Volume, Is.EqualTo(3f).Within(0.0001f));
    }

    [Test]
    [RunOnSide(Side.Server)]
    public void CPRVerbStartsAssistanceOnThePatient()
    {
        var rescuer = SSpawn("MobHuman");
        var patient = SSpawn("MobHuman");
        _mob.ChangeMobState(patient, MobState.Critical);
        var ev = new GetVerbsEvent<InnateVerb>(rescuer, patient, null, null, true, true, true, new());
        SEntMan.EventBus.RaiseLocalEvent(rescuer, ev);
        var cpr = ev.Verbs.Single(x => x.Text == Loc.GetString("cpr-verb"));
        cpr.Act!.Invoke();
        Assert.That(SEntMan.HasComponent<ActiveCPRComponent>(patient), Is.True);
        Assert.That(SEntMan.HasComponent<ActiveCPRComponent>(rescuer), Is.False);
    }

    [Test]
    [RunOnSide(Side.Server)]
    public void AnalyzerIncludesBodyAndBleedingData()
    {
        var body = SSpawn("MobHuman");
        var arm = _body.GetOrgan(body, Arm)!.Value;
        _wounds.TryInduceWound(arm, Slash, 5, out _, damageGroup: Brute);
        var state = _analyzer.GetHealthAnalyzerUiState(body, true);
        Assert.Multiple(() =>
        {
            Assert.That(state.Body, Does.ContainKey(Arm));
            Assert.That(state.Bleeding, Does.Contain(Arm));
            Assert.That(state.VitalDamage, Is.EqualTo(_threshold.CheckVitalDamage(body)));
        });
    }
    [Test]
    [RunOnSide(Side.Server)]
    public void BloodpackRepeatsUntilBloodIsRestored()
    {
        var body = SSpawn("MobHuman");
        _blood.TryModifyBloodLevel(body, -200);
        var item = SSpawn("Bloodpack");
        var before = SComp<StackComponent>(item).Count;
        var ev = FinishTreatment(body, item);
        Assert.Multiple(() =>
        {
            Assert.That(ev.Repeat, Is.True);
            Assert.That(SComp<StackComponent>(item).Count, Is.EqualTo(before - 1));
        });
    }

    [TestCase(true, false)]
    [TestCase(false, true)]
    [TestCase(true, true)]
    [RunOnSide(Side.Server)]
    public void CPRRequiresBothMouthsAvailable(bool blockRescuer, bool blockPatient)
    {
        var rescuer = SSpawn("MobHuman");
        var patient = SSpawn("MobHuman");
        _mob.ChangeMobState(patient, MobState.Critical);
        if (blockRescuer)
            Assert.That(_inventory.TryEquip(rescuer, SSpawn("ClothingMaskGas"), "mask", force: true), Is.True);
        if (blockPatient)
            Assert.That(_inventory.TryEquip(patient, SSpawn("ClothingMaskGas"), "mask", force: true), Is.True);
        var ev = new GetVerbsEvent<InnateVerb>(rescuer, patient, null, null, true, true, true, new());
        SEntMan.EventBus.RaiseLocalEvent(rescuer, ev);
        ev.Verbs.Single(x => x.Text == Loc.GetString("cpr-verb")).Act!.Invoke();
        Assert.That(SEntMan.HasComponent<ActiveCPRComponent>(patient), Is.False);
    }

    [Test]
    [RunOnSide(Side.Server)]
    public void PartialHealingStillConsumesADoseWhenAnotherPartIsBlocked()
    {
        var body = SSpawn("MobHuman");
        var arm = _body.GetOrgan(body, Arm)!.Value;
        var head = _body.GetOrgan(body, Head)!.Value;
        _damage.ChangeDamage(body, new DamageSpecifier { DamageDict = { ["Blunt"] = 5 } },
            true, targetPart: TargetBodyPart.Head, canMiss: false);
        _wounds.TryInduceWound(arm, Slash, 20, out _, damageGroup: Brute);
        _targeting.SetTarget(body, TargetBodyPart.LeftArm);
        var item = SSpawn("Brutepack");
        var before = SComp<StackComponent>(item).Count;
        var ev = FinishTreatment(body, item);
        Assert.Multiple(() =>
        {
            Assert.That(_damage.GetTotalDamage(head), Is.EqualTo(FixedPoint2.Zero));
            Assert.That(ev.Handled, Is.True);
            Assert.That(SComp<StackComponent>(item).Count, Is.EqualTo(before - 1));
        });
    }

    [TestCase(0.5f, 0.5f)]
    [TestCase(1f, 1f)]
    [TestCase(2f, 2f)]
    [RunOnSide(Side.Server)]
    public void TemperatureScaledMedicineScalesDoseOnlyOnce(float dose, float expectedHealing)
    {
        var body = SSpawn("MobHuman");
        var arm = _body.GetOrgan(body, Arm)!.Value;
        _damage.ChangeDamage(body, new DamageSpecifier { DamageDict = { ["Blunt"] = 10 } },
            true, targetPart: TargetBodyPart.LeftArm, canMiss: false);
        var temperature = FixedPoint2.New(SComp<TemperatureComponent>(body).Temperature);
        _effects.ApplyEffect(body, new HealthChange
        {
            Damage = new DamageSpecifier { DamageDict = { ["Blunt"] = -1 } },
            ScaleByTemperature = new TemperatureScaling(temperature - 100, temperature + 100, 2),
            TargetPart = TargetBodyPart.LeftArm,
        }, dose);
        Assert.That(_damage.GetTotalDamage(arm), Is.EqualTo((FixedPoint2) (10 - expectedHealing)));
    }

    [Test]
    [RunOnSide(Side.Server)]
    public void CryoAnalyzerSelectsOnlyItsCurrentPatientsParts()
    {
        var pod = SSpawn("CryoPod");
        var patient = SSpawn("MobHuman");
        var cryo = SComp<CryoPodComponent>(pod);
        Assert.That(_containers.Insert(patient, cryo.BodyContainer), Is.True);
        var message = new HealthAnalyzerPartMessage(Arm);
        SEntMan.EventBus.RaiseLocalEvent(pod, message);
        var analyzer = SComp<HealthAnalyzerComponent>(pod);
        Assert.That(_analyzer.GetAnalyzedPart((pod, analyzer), patient), Is.EqualTo(_body.GetOrgan(patient, Arm)));
        Assert.That(_containers.Remove(patient, cryo.BodyContainer), Is.True);
        Assert.That(_analyzer.GetAnalyzedPart((pod, analyzer), null), Is.Null);
    }

    [Test]
    [RunOnSide(Side.Server)]
    public void MultiverRemovesForeignReagentsFromBlood()
    {
        var body = SSpawn("MobHuman");
        _blood.TryModifyBloodLevel(body, -20);
        Assert.That(_blood.TryAddToBloodstream(body, new Solution("Toxin", 10)), Is.True);
        ProtoId<ReagentPrototype> multiver = "Multiver";
        var effects = _prototypes.Index(multiver).Metabolisms!.Metabolisms["Bloodstream"].Effects;
        _effects.TryApplyEffects(body, effects);
        var bloodstream = SComp<BloodstreamComponent>(body);
        Assert.That(_solutions.ResolveSolution(body, bloodstream.BloodSolutionName, ref bloodstream.BloodSolution, out var solution), Is.True);
        Assert.That(solution.GetTotalPrototypeQuantity("Toxin"), Is.EqualTo((FixedPoint2) 7));
    }

}
