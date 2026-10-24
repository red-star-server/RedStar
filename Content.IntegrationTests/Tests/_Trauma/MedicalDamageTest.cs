using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.Shared._Trauma.Common.Targeting;
using Content.Shared._Trauma.Wounds;
using Content.Shared.Body;
using Content.Shared.Projectiles;
using Content.Shared.Body.Components;
using Content.Shared.Inventory;
using Content.Shared._Trauma.Common.Body;
using Content.Shared._Trauma.Traumas;
using Content.Shared.Rejuvenate;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests._Trauma;

[TestFixture]
public sealed class MedicalDamageTest : GameTest
{
    [SidedDependency(Side.Server)] private BodySystem _body = default!;
    [SidedDependency(Side.Server)] private DamageableSystem _damage = default!;
    [SidedDependency(Side.Server)] private WoundSystem _wounds = default!;

    [SidedDependency(Side.Server)] private TraumaSystem _traumas = default!;

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
";

    private static readonly EntProtoId BluntWound = "WoundBlunt";

    [TestCase("MobHuman")]
    [TestCase("MobDwarf")]
    [TestCase("MobArachnid")]
    [TestCase("MobDiona")]
    [TestCase("MobGingerbread")]
    [TestCase("MobMoth")]
    [TestCase("MobReptilian")]
    [TestCase("MobSkeletonPerson")]
    [TestCase("MobSlimePerson")]
    [TestCase("MobVox")]
    [TestCase("MobVulpkanin")]
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
        Assert.That(_wounds.TryHealBleedingWounds(arm, 100, out _), Is.True);
        SEntMan.EventBus.RaiseLocalEvent(body, ref update);
        Assert.That(blood.BleedAmountFromWounds, Is.Zero);
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

    [Test]
    public void DamageArithmeticPreservesIndependentMetadata()
    {
        var damage = new DamageSpecifier
        {
            DamageDict = { ["Blunt"] = 10 },
            ArmorPenetration = 0.25f,
            PartDamageVariation = 2f,
            Flags = DamageSpecifier.DamageFlags.PreciseHit,
            WoundSeverityMultipliers = { ["Blunt"] = 2 },
            WoundTypeOverrides = { ["Blunt"] = "WoundBlunt" },
        };
        var scaled = damage * 2;
        Assert.That(scaled.DamageDict["Blunt"], Is.EqualTo((FixedPoint2) 20));
        Assert.That(scaled.ArmorPenetration, Is.EqualTo(damage.ArmorPenetration));
        Assert.That(scaled.PartDamageVariation, Is.EqualTo(damage.PartDamageVariation));
        Assert.That(scaled.Flags, Is.EqualTo(damage.Flags));
        Assert.That(scaled.WoundSeverityMultipliers, Is.EqualTo(damage.WoundSeverityMultipliers));
        scaled.WoundSeverityMultipliers["Blunt"] = 3;
        Assert.That(damage.WoundSeverityMultipliers["Blunt"], Is.EqualTo((FixedPoint2) 2));
    }
}
