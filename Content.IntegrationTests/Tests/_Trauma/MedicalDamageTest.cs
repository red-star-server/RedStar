using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.Shared._Trauma.Common.Body;
using Content.Shared._Trauma.Common.Targeting;
using Content.Shared._Trauma.Targeting;
using Content.Shared._Trauma.Traumas;
using Content.Shared._Trauma.Wounds;
using Content.Shared.Body;
using Content.Shared.Body.Components;
using Content.Shared.CCVar;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Explosion;
using Content.Shared.Eye.Blinding.Components;
using Content.Shared.Eye.Blinding.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.Inventory;
using Content.Shared.Projectiles;
using Content.Shared.Rejuvenate;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests._Trauma;

[TestFixture]
public sealed class MedicalDamageTest : GameTest
{
    [SidedDependency(Side.Server)] private BodySystem _body = default!;
    [SidedDependency(Side.Server)] private DamageableSystem _damage = default!;
    [SidedDependency(Side.Server)] private WoundSystem _wounds = default!;

    [SidedDependency(Side.Server)] private TraumaSystem _traumas = default!;
    [SidedDependency(Side.Server)] private IConfigurationManager _cfg = default!;
    [SidedDependency(Side.Server)] private BlindableSystem _blindable = default!;
    [SidedDependency(Side.Server)] private SharedTargetingSystem _targeting = default!;
    [SidedDependency(Side.Server)] private InventorySystem _inventory = default!;

    [TestPrototypes]
    private const string Prototypes = @"
- type: entity
  id: TraumaDamageTestLegacyBody
  components:
  - type: Body
  - type: Damageable
  - type: Injurable
    damageContainer: Biological
- type: entity
  id: TraumaDamageTestArmor
  components:
  - type: Armor
    coverage: [Torso]
    modifiers:
      coefficients:
        Blunt: 0.5

- type: damageModifierSet
  id: TraumaTestVitalResistance
  coefficients:
    Asphyxiation: 0.5

- type: entity
  id: TraumaTestDamageBody
  components:
  - type: Body
  - type: InitialBody
    organs:
      Torso: TraumaTestDamageTorso
      Head: TraumaTestDamageHead
  - type: Damageable
  - type: Injurable
    damageContainer: Biological

- type: entity
  id: TraumaTestDamageTorso
  components:
  - type: Organ
    category: Torso
  - type: ParentOrgan
  - type: BodyPart
    partType: Torso
    slots: [Head]
  - type: Damageable
    damageModifierSet: TraumaTestVitalResistance
  - type: Injurable
    damageContainer: Biological

- type: entity
  id: TraumaTestDamageHead
  components:
  - type: Organ
    category: Head
  - type: ChildOrgan
    parents: [Torso]
  - type: BodyPart
    partType: Head
    slots: []
  - type: Damageable
    damageModifierSet: TraumaTestVitalResistance
  - type: Injurable
    damageContainer: Biological

- type: entity
  id: TraumaTestInitiallyDamagedBody
  parent: TraumaTestDamageBody
  components:
  - type: Damageable
    damage:
      types:
        Blunt: 12
";

    private static readonly EntProtoId BluntWound = "WoundBlunt";

    [TestCase("MobHuman")]
    [TestCase("MobDiona")]
    [TestCase("MobSlimePerson")]
    [RunOnSide(Side.Server)]
    public void TargetedDamageChangesOnlySelectedPart(string prototype)
    {
        var body = SSpawn(prototype);
        var arm = _body.GetOrgan(body, "ArmLeft").Value;
        var head = _body.GetOrgan(body, "Head").Value;
        var beforeHead = _damage.GetTotalDamage(head);
        var beforeArm = _damage.GetTotalDamage(arm);
        var damage = new DamageSpecifier { DamageDict = { ["Blunt"] = 5 } };
        var result = _damage.ChangeDamage(body, damage, ignoreResistances: true,
            targetPart: TargetBodyPart.LeftArm, canMiss: false);
        Assert.That(result.GetTotal(), Is.GreaterThan(FixedPoint2.Zero));
        Assert.That(_damage.GetTotalDamage(arm), Is.GreaterThan(beforeArm));
        Assert.That(_damage.GetTotalDamage(head), Is.EqualTo(beforeHead));
        Assert.That(_damage.GetTotalDamage(body), Is.GreaterThan(FixedPoint2.Zero));
    }

    [Test]
    [RunOnSide(Side.Server)]
    public void BluntWoundCanBeCreatedContinuedAndHealed()
    {
        var body = SSpawn("MobHuman");
        var arm = _body.GetOrgan(body, "ArmLeft").Value;
        Assert.That(_wounds.TryInduceWound(arm, BluntWound, 10, out var wound), Is.True);
        Assert.That(wound.HasValue, Is.True);
        Assert.That(_wounds.TryContinueWound(arm, BluntWound, 5, out var continued), Is.True);
        Assert.That(continued.Value.Owner, Is.EqualTo(wound.Value.Owner));
        Assert.That(_wounds.TryHealWounds(arm, 5, out var healed), Is.True);
        Assert.That(healed, Is.EqualTo((FixedPoint2) 5));
    }

    [Test]
    [RunOnSide(Side.Server)]
    public void LegacyBodyStillTakesAndHealsDamage()
    {
        var body = SSpawn("TraumaDamageTestLegacyBody");
        var damage = new DamageSpecifier { DamageDict = { ["Blunt"] = 10 } };
        Assert.That(_damage.ChangeDamage(body, damage).GetTotal(), Is.EqualTo((FixedPoint2) 10));
        Assert.That(_damage.GetTotalDamage(body), Is.EqualTo((FixedPoint2) 10));
        _damage.ChangeDamage(body, damage * -1);
        Assert.That(_damage.GetTotalDamage(body), Is.EqualTo(FixedPoint2.Zero));
    }

    [TestCase(BodyPartType.Torso, 0f, 5)]
    [TestCase(BodyPartType.Head, 0f, 10)]
    [TestCase(BodyPartType.Torso, 1f, 10)]
    [RunOnSide(Side.Server)]
    public void ArmorRespectsCoverageAndPenetration(BodyPartType part, float penetration, int expected)
    {
        var armor = SSpawn("TraumaDamageTestArmor");
        var damage = new DamageSpecifier { DamageDict = { ["Blunt"] = 10 }, ArmorPenetration = penetration };
        var modify = new DamageModifyEvent(armor, damage, targetPart: part);
        var relay = new InventoryRelayedEvent<DamageModifyEvent>(modify, armor);
        SEntMan.EventBus.RaiseLocalEvent(armor, relay);
        Assert.That(modify.Damage.GetTotal(), Is.EqualTo((FixedPoint2) expected));
    }

    [Test]
    [RunOnSide(Side.Server)]
    public void RejuvenationRestoresBoneAndRemovesWounds()
    {
        var body = SSpawn("MobHuman");
        var arm = _body.GetOrgan(body, "ArmLeft").Value;
        var bone = SComp<BoneComponent>(arm);
        Assert.That(_traumas.DamageBone(arm, 10), Is.True);
        Assert.That(_wounds.TryInduceWound(arm, BluntWound, 10, out _), Is.True);
        var rejuvenate = new RejuvenateEvent();
        SEntMan.EventBus.RaiseLocalEvent(body, rejuvenate);
        Assert.That(bone.BoneIntegrity, Is.EqualTo(bone.IntegrityCap));
        Assert.That(SComp<WoundableComponent>(arm).Wounds.ContainedEntities, Is.Empty);
        Assert.That(SComp<WoundableComponent>(arm).Integrity,
            Is.EqualTo(SComp<WoundableComponent>(arm).IntegrityCap));
    }

    [Test]
    [RunOnSide(Side.Server)]
    public void WoundBleedingReachesBodyAndStopsAfterHealing()
    {
        var body = SSpawn("MobHuman");
        var arm = _body.GetOrgan(body, "ArmLeft").Value;
        EntProtoId slash = "Slash";
        Assert.That(_wounds.TryInduceWound(arm, slash, 10, out _), Is.True);
        var update = new BloodstreamUpdateEvent();
        SEntMan.EventBus.RaiseLocalEvent(body, ref update);
        var blood = SComp<BloodstreamComponent>(body);
        Assert.That(blood.BleedAmountFromWounds, Is.GreaterThan(0f));
        Assert.That(blood.BleedAmountNotFromWounds, Is.Zero);
        Assert.That(blood.BleedAmount, Is.EqualTo(blood.BleedAmountFromWounds));
        Assert.That(_wounds.TryHealBleedingWounds(arm, 100, out _), Is.True);
        SEntMan.EventBus.RaiseLocalEvent(body, ref update);
        Assert.That(blood.BleedAmountFromWounds, Is.Zero);
        Assert.That(blood.BleedAmount, Is.Zero);
    }

    [TestCase(false, 10)]
    [TestCase(true, 20)]
    [RunOnSide(Side.Server)]
    public void SplitDamagePreservesIncreaseOnly(bool increaseOnly, int expected)
    {
        var body = SSpawn("TraumaTestDamageBody");
        var damage = new DamageSpecifier { DamageDict = { ["Asphyxiation"] = 20 } };
        var applied = _damage.ChangeDamage(body, damage, increaseOnly: increaseOnly);
        Assert.That(applied.GetTotal(), Is.EqualTo((FixedPoint2) expected));
        Assert.That(_damage.GetTotalDamage(body), Is.EqualTo((FixedPoint2) expected));
    }

    [TestCase(false, 40)]
    [TestCase(true, 20)]
    [RunOnSide(Side.Server)]
    public void SplitDamagePreservesGlobalModifierOptOut(bool ignoreGlobalModifiers, int expected)
    {
        var previous = _cfg.GetCVar(CCVars.PlaytestAllDamageModifier);
        try
        {
            _cfg.SetCVar(CCVars.PlaytestAllDamageModifier, 2f);
            var body = SSpawn("TraumaTestDamageBody");
            var damage = new DamageSpecifier { DamageDict = { ["Blunt"] = 20 } };
            var applied = _damage.ChangeDamage(body, damage, ignoreResistances: true,
                targetPart: TargetBodyPart.Vital, ignoreGlobalModifiers: ignoreGlobalModifiers);
            Assert.That(applied.GetTotal(), Is.EqualTo((FixedPoint2) expected));
        }
        finally
        {
            _cfg.SetCVar(CCVars.PlaytestAllDamageModifier, previous);
        }
    }

    [Test]
    [RunOnSide(Side.Server)]
    public void InitialBodyDamageIsTransferredToParts()
    {
        var body = SSpawn("TraumaTestInitiallyDamagedBody");
        Assert.That(_damage.GetTotalDamage(body), Is.EqualTo((FixedPoint2) 12));
        var torso = _body.GetOrgan(body, "Torso").Value;
        Assert.That(_damage.GetTotalDamage(torso), Is.EqualTo((FixedPoint2) 12));
    }

    [Test]
    [RunOnSide(Side.Server)]
    public void EquippedHelmetProtectsOnlyCoveredPartsFromExplosions()
    {
        var body = SSpawn("MobHuman");
        var helmet = SSpawn("ClothingHeadHelmetHardsuitEngineering");
        Assert.That(_inventory.TryEquip(body, helmet, "head", force: true), Is.True);
        var head = _body.GetOrgan(body, "Head").Value;
        var arm = _body.GetOrgan(body, "ArmLeft").Value;
        var headResistance = new GetExplosionResistanceEvent("Default");
        SEntMan.EventBus.RaiseLocalEvent(head, ref headResistance);
        var armResistance = new GetExplosionResistanceEvent("Default");
        SEntMan.EventBus.RaiseLocalEvent(arm, ref armResistance);
        Assert.That(headResistance.DamageCoefficient, Is.EqualTo(0.5f));
        Assert.That(armResistance.DamageCoefficient, Is.EqualTo(1f));
    }

    [Test]
    [RunOnSide(Side.Server)]
    public void InternalOrganDamageReducesIntegrityAndRemovalRestoresIt()
    {
        var heart = SSpawn("OrganHumanHeart");
        var organ = SComp<InternalChildOrganComponent>(heart);
        Assert.That(_traumas.TryCreateOrganDamageModifier(heart, 2, heart, "TestDamage"), Is.True);
        Assert.That(organ.OrganIntegrity, Is.EqualTo(organ.IntegrityCap - 2));
        Assert.That(_traumas.TryChangeOrganDamageModifier(heart, 1, heart, "TestDamage"), Is.True);
        Assert.That(organ.OrganIntegrity, Is.EqualTo(organ.IntegrityCap - 3));
        Assert.That(_traumas.TryRemoveOrganDamageModifier(heart, heart, "TestDamage"), Is.True);
        Assert.That(organ.OrganIntegrity, Is.EqualTo(organ.IntegrityCap));
    }

    [Test]
    [RunOnSide(Side.Server)]
    public void DestroyedInternalOrganIsRemovedFromItsParentPart()
    {
        var body = SSpawn("MobHuman");
        var heart = _body.GetOrgan(body, "Heart").Value;
        var organ = SComp<InternalChildOrganComponent>(heart);
        Assert.That(_traumas.TryCreateOrganDamageModifier(heart, organ.IntegrityCap, body, "TestDamage"), Is.True);
        Assert.That(_body.GetOrgan(body, "Heart"), Is.Null);
    }

    [Test]
    [RunOnSide(Side.Server)]
    public void EyeDamageAccumulatesAndHealingRestoresOrganIntegrity()
    {
        var body = SSpawn("MobHuman");
        var eyes = _body.GetOrgan(body, "Eyes").Value;
        var organ = SComp<InternalChildOrganComponent>(eyes);
        _blindable.AdjustEyeDamage(body, 2);
        Assert.That(organ.OrganIntegrity, Is.EqualTo(organ.IntegrityCap - 2));
        _blindable.AdjustEyeDamage(body, 1);
        Assert.That(organ.OrganIntegrity, Is.EqualTo(organ.IntegrityCap - 3));
        Assert.That(SComp<BlindableComponent>(body).EyeDamage, Is.EqualTo(3));
        _blindable.AdjustEyeDamage(body, -10);
        Assert.That(organ.OrganIntegrity, Is.EqualTo(organ.IntegrityCap));
        Assert.That(SComp<BlindableComponent>(body).EyeDamage, Is.Zero);
        _blindable.AdjustEyeDamage(body, 1);
        Assert.That(organ.OrganIntegrity, Is.EqualTo(organ.IntegrityCap - 1));
        Assert.That(SComp<BlindableComponent>(body).EyeDamage, Is.EqualTo(1));
    }

    [TestCase((TargetBodyPart) 0, TargetBodyPart.Chest)]
    [TestCase(TargetBodyPart.All, TargetBodyPart.Chest)]
    [TestCase((TargetBodyPart) ushort.MaxValue, TargetBodyPart.Chest)]
    [TestCase(TargetBodyPart.Head, TargetBodyPart.Head)]
    [RunOnSide(Side.Server)]
    public void TargetSelectionRejectsInvalidNetworkValues(TargetBodyPart requested, TargetBodyPart expected)
    {
        var body = SSpawn("MobHuman");
        _targeting.SetTarget(body, requested);
        Assert.That(SComp<TargetingComponent>(body).Target, Is.EqualTo(expected));
    }

    [Test]
    [RunOnSide(Side.Server)]
    public void VitalDamagePreservesSpecifierMetadata()
    {
        var damage = new DamageSpecifier
        {
            DamageDict = { ["Asphyxiation"] = 2, ["Blunt"] = 4 },
            ArmorPenetration = 0.25f,
            Flags = DamageSpecifier.DamageFlags.PreciseHit,
            WoundSeverityMultipliers = { ["Asphyxiation"] = 2 },
        };
        var vital = _damage.GetVitalDamage(damage);
        Assert.That(vital.GetTotal(), Is.EqualTo((FixedPoint2) 2));
        Assert.That(vital.ArmorPenetration, Is.EqualTo(damage.ArmorPenetration));
        Assert.That(vital.Flags, Is.EqualTo(damage.Flags));
        Assert.That(vital.WoundSeverityMultipliers, Is.EqualTo(damage.WoundSeverityMultipliers));
    }

    [TestCase(1f)]
    [TestCase(1.4f)]
    [RunOnSide(Side.Server)]
    public void PartialBleedingTreatmentReducesScaledBleeding(float scaling)
    {
        var body = SSpawn("MobHuman");
        var arm = _body.GetOrgan(body, "ArmLeft").Value;
        EntProtoId slash = "Slash";
        Assert.That(_wounds.TryInduceWound(arm, slash, 10, out var wound), Is.True);
        var bleeds = SComp<BleedInflicterComponent>(wound.Value.Owner);
        bleeds.Scaling = scaling;
        var previous = bleeds.BleedingAmount;
        Assert.That(_wounds.TryHealBleedingWounds(arm, 0.5, out var healed), Is.True);
        Assert.That(bleeds.BleedingAmount, Is.LessThan(previous));
        Assert.That(healed, Is.GreaterThan(FixedPoint2.Zero).And.LessThanOrEqualTo((FixedPoint2) 0.5));
        Assert.That(previous - bleeds.BleedingAmount, Is.EqualTo(healed));
    }

    [Test]
    [RunOnSide(Side.Server)]
    public void CauterizationReducesWoundBleeding()
    {
        var body = SSpawn("MobHuman");
        var arm = _body.GetOrgan(body, "ArmLeft").Value;
        EntProtoId slash = "Slash";
        Assert.That(_wounds.TryInduceWound(arm, slash, 10, out var wound), Is.True);
        var bleeds = SComp<BleedInflicterComponent>(wound.Value.Owner);
        var previous = bleeds.BleedingAmount;
        var cauterize = new CauterizedEvent(0.5f);
        SEntMan.EventBus.RaiseLocalEvent(body, ref cauterize);
        Assert.That(bleeds.BleedingAmount, Is.LessThan(previous));
    }

    [Test]
    [RunOnSide(Side.Server)]
    public void RejuvenationRestoresInternalOrganIntegrity()
    {
        var body = SSpawn("MobHuman");
        var heart = _body.GetOrgan(body, "Heart").Value;
        Assert.That(_traumas.TryCreateOrganDamageModifier(heart, 2, body, "TestDamage"), Is.True);
        _blindable.AdjustEyeDamage(body, 2);
        var rejuvenate = new RejuvenateEvent();
        SEntMan.EventBus.RaiseLocalEvent(body, rejuvenate);
        var organ = SComp<InternalChildOrganComponent>(_body.GetOrgan(body, "Heart").Value);
        Assert.That(organ.OrganIntegrity, Is.EqualTo(organ.IntegrityCap));
        Assert.That(organ.IntegrityModifiers, Is.Empty);
        Assert.That(SComp<BlindableComponent>(body).EyeDamage, Is.Zero);
    }

    [Test]
    [RunOnSide(Side.Server)]
    public void BulletDamageCreatesBallisticWoundsOnBodyParts()
    {
        var body = SSpawn("MobHuman");
        var bullet = SSpawn("BulletPistol");
        var arm = _body.GetOrgan(body, "ArmLeft").Value;
        var damage = SComp<ProjectileComponent>(bullet).Damage;
        Assert.That(damage.DamageDict.ContainsKey("Ballistic"), Is.True);
        Assert.That(damage.DamageDict.ContainsKey("Piercing"), Is.False);
        var applied = _damage.ChangeDamage(body, damage, ignoreResistances: true,
            targetPart: TargetBodyPart.LeftArm, canMiss: false);
        Assert.That(applied.GetTotal(), Is.GreaterThan(FixedPoint2.Zero));
        Assert.That(_wounds.GetWoundableWounds(arm), Is.Not.Empty);
        Assert.That(_damage.GetTotalDamage(arm), Is.EqualTo(applied.GetTotal()));
    }

}
