using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.Shared._Trauma.Body;
using Content.Shared._Trauma.Common.Body;
using Content.Shared._Trauma.DelayedDeath;
using Content.Shared.Body;
using Content.Shared.Body.Components;
using Content.Shared.Eye.Blinding.Components;
using Robust.Shared.Containers;

namespace Content.IntegrationTests.Tests._Trauma;

[TestFixture]
[TestOf(typeof(BodyPartSystem))]
public sealed class BodyTest : GameTest
{
    [TestPrototypes]
    private const string Prototypes = @"
- type: entity
  id: TraumaTestBody
  components:
  - type: Body
  - type: InitialBody
    organs:
      Torso: TraumaTestTorso
      Head: TraumaTestHead
      Brain: TraumaTestBrain
      Heart: TraumaTestHeart
      ArmLeft: TraumaTestArm
      HandLeft: TraumaTestHand

- type: entity
  id: TraumaTestTorso
  components:
  - type: Organ
    category: Torso
  - type: ParentOrgan
  - type: BodyPart
    partType: Torso
    slots: [Head, Heart, ArmLeft]

- type: entity
  id: TraumaTestHead
  components:
  - type: Organ
    category: Head
  - type: ParentOrgan
  - type: ChildOrgan
    parents: [Torso]
  - type: BodyPart
    partType: Head
    slots: [Brain]

- type: entity
  id: TraumaTestBrain
  components:
  - type: Organ
    category: Brain
  - type: ChildOrgan
    parents: [Head]
  - type: InternalChildOrgan
  - type: Brain

- type: entity
  id: TraumaTestHeart
  components:
  - type: Organ
    category: Heart
  - type: ChildOrgan
    parents: [Torso]
  - type: InternalChildOrgan
  - type: Heart

- type: entity
  id: TraumaTestArm
  components:
  - type: Organ
    category: ArmLeft
  - type: ParentOrgan
  - type: ChildOrgan
    parents: [Torso]
  - type: BodyPart
    partType: Arm
    symmetry: Left
    slots: [HandLeft]

- type: entity
  id: TraumaTestHand
  components:
  - type: Organ
    category: HandLeft
  - type: ChildOrgan
    parents: [ArmLeft]
  - type: BodyPart
    partType: Hand
    symmetry: Left
    slots: []
";

    [SidedDependency(Side.Server)] private BodySystem _body = default!;
    [SidedDependency(Side.Server)] private BodyPartSystem _parts = default!;
    [SidedDependency(Side.Server)] private BodyRestoreSystem _restore = default!;
    [SidedDependency(Side.Server)] private SharedContainerSystem _containers = default!;

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
    public void PlayableSpeciesUseTraumaAnatomy(string prototype)
    {
        var body = SSpawn(prototype);
        var root = _parts.GetRootPart(body);
        Assert.That(root, Is.Not.Null);
        Assert.That(_body.GetCategory(root.Value.Owner).Value.Id, Is.EqualTo("Torso"));

        var cache = SComp<BodyCacheComponent>(body);
        Assert.That(cache.Organs.Count, Is.EqualTo(SComp<InitialBodyComponent>(body).Organs.Count));
        foreach (var (category, organ) in cache.Organs)
        {
            Assert.That(_body.GetBody(organ), Is.EqualTo(body), category.ToString());
            Assert.That(SEntMan.HasComponent<EnabledOrganComponent>(organ), Is.True, category.ToString());
            if (!SEntMan.TryGetComponent<ChildOrganComponent>(organ, out var child))
                continue;

            Assert.That(child.Parent, Is.Not.Null, category.ToString());
            Assert.That(_parts.GetOrgan(child.Parent.Value, category), Is.EqualTo(organ));
        }
    }

    [Test]
    [RunOnSide(Side.Server)]
    public void HumanArmCanBeRemovedAndReattached()
    {
        var body = SSpawn("MobHuman");
        var arm = _body.GetOrgan(body, "ArmLeft").Value;
        var hand = _body.GetOrgan(body, "HandLeft").Value;
        Assert.That(_body.RemoveOrgan(body, arm), Is.True);
        Assert.That(_body.GetOrgan(body, "HandLeft"), Is.Null);
        Assert.That(_parts.GetSeveredOrgansContainer(arm).ContainedEntities, Does.Contain(hand));

        Assert.That(_body.InsertOrgan(body, arm), Is.True);
        Assert.That(_body.GetOrgan(body, "HandLeft"), Is.EqualTo(hand));
        Assert.That(SComp<ChildOrganComponent>(hand).Parent, Is.EqualTo(arm));
    }

    [Test]
    [RunOnSide(Side.Server)]
    public void MissingEyesBlindHumanUntilReinserted()
    {
        var body = SSpawn("MobHuman");
        var eyes = _body.GetOrgan(body, "Eyes").Value;
        Assert.That(_body.RemoveOrgan(body, eyes), Is.True);
        Assert.That(SComp<BlindableComponent>(body).IsBlind, Is.True);

        Assert.That(_body.InsertOrgan(body, eyes), Is.True);
        Assert.That(SComp<BlindableComponent>(body).EyeDamage, Is.Zero);
        Assert.That(SComp<BlindableComponent>(body).IsBlind, Is.False);
    }

    [Test]
    [RunOnSide(Side.Server)]
    public void SlimeCoreRemainsInTorsoAfterHeadRemoval()
    {
        var body = SSpawn("MobSlimePerson");
        var torso = _body.GetOrgan(body, "Torso").Value;
        var brain = _body.GetOrgan(body, "Brain").Value;
        var head = _body.GetOrgan(body, "Head").Value;

        Assert.That(SComp<ChildOrganComponent>(brain).Parent, Is.EqualTo(torso));
        Assert.That(_body.RemoveOrgan(body, head), Is.True);
        Assert.That(_body.GetOrgan(body, "Brain"), Is.EqualTo(brain));
        Assert.That(SEntMan.HasComponent<DebrainedComponent>(body), Is.False);
    }

    [Test]
    [RunOnSide(Side.Server)]
    public void InitialBodyCachesAndParentsOrgans()
    {
        var body = SSpawn("TraumaTestBody");
        var torso = _body.GetOrgan(body, "Torso").Value;
        var arm = _body.GetOrgan(body, "ArmLeft").Value;
        var hand = _body.GetOrgan(body, "HandLeft").Value;

        Assert.That(SComp<BodyCacheComponent>(body).Organs, Has.Count.EqualTo(6));
        Assert.That(SComp<ChildOrganComponent>(arm).Parent, Is.EqualTo(torso));
        Assert.That(SComp<ChildOrganComponent>(hand).Parent, Is.EqualTo(arm));
        Assert.That(_parts.GetOrgan(arm, "HandLeft"), Is.EqualTo(hand));
        Assert.That(SEntMan.HasComponent<EnabledOrganComponent>(hand), Is.True);
    }

    [Test]
    [RunOnSide(Side.Server)]
    public void DetachedArmRetainsAndRestoresHand()
    {
        var body = SSpawn("TraumaTestBody");
        var arm = _body.GetOrgan(body, "ArmLeft").Value;
        var hand = _body.GetOrgan(body, "HandLeft").Value;

        Assert.That(_body.RemoveOrgan(body, arm), Is.True);
        Assert.That(_body.GetOrgan(body, "ArmLeft"), Is.Null);
        Assert.That(_body.GetOrgan(body, "HandLeft"), Is.Null);
        Assert.That(_parts.GetSeveredOrgansContainer(arm).ContainedEntities, Does.Contain(hand));
        Assert.That(SEntMan.HasComponent<EnabledOrganComponent>(hand), Is.False);

        Assert.That(_body.InsertOrgan(body, arm), Is.True);
        Assert.That(_body.GetOrgan(body, "HandLeft"), Is.EqualTo(hand));
        Assert.That(SComp<ChildOrganComponent>(hand).Parent, Is.EqualTo(arm));
        Assert.That(_parts.GetOrgan(arm, "HandLeft"), Is.EqualTo(hand));
        Assert.That(SEntMan.HasComponent<EnabledOrganComponent>(hand), Is.True);
    }

    [Test]
    [RunOnSide(Side.Server)]
    public void MissingParentAndOccupiedSlotRejectInsertion()
    {
        var body = SSpawn("TraumaTestBody");
        var spare = SSpawn("TraumaTestHand");
        Assert.That(_body.InsertOrgan(body, spare), Is.False);

        var arm = _body.GetOrgan(body, "ArmLeft").Value;
        Assert.That(_body.RemoveOrgan(body, arm), Is.True);
        Assert.That(_body.InsertOrgan(body, spare), Is.False);
    }

    [Test]
    [RunOnSide(Side.Server)]
    public void RestoringBodyRecreatesMissingSubtree()
    {
        var body = SSpawn("TraumaTestBody");
        var arm = _body.GetOrgan(body, "ArmLeft").Value;
        Assert.That(_body.RemoveOrgan(body, arm), Is.True);

        _restore.RestoreBody(body);

        var restoredArm = _body.GetOrgan(body, "ArmLeft").Value;
        var restoredHand = _body.GetOrgan(body, "HandLeft").Value;
        Assert.That(restoredArm, Is.Not.EqualTo(arm));
        Assert.That(_parts.GetOrgan(restoredArm, "HandLeft"), Is.EqualTo(restoredHand));
        Assert.That(SComp<BodyCacheComponent>(body).Organs, Has.Count.EqualTo(6));
    }

    [Test]
    [RunOnSide(Side.Server)]
    public void RemovingAndReplacingHeartControlsDeathTimer()
    {
        var body = SSpawn("TraumaTestBody");
        var heart = _body.GetOrgan(body, "Heart").Value;
        Assert.That(_body.RemoveOrgan(body, heart), Is.True);
        Assert.That(SEntMan.HasComponent<DelayedDeathComponent>(body), Is.True);

        Assert.That(_body.InsertOrgan(body, heart), Is.True);
        Assert.That(SEntMan.HasComponent<DelayedDeathComponent>(body), Is.False);
    }

    [Test]
    [RunOnSide(Side.Server)]
    public void RemovingHeadMovesBrainAndMarksBodyDebrained()
    {
        var body = SSpawn("TraumaTestBody");
        var head = _body.GetOrgan(body, "Head").Value;
        var brain = _body.GetOrgan(body, "Brain").Value;

        Assert.That(_body.RemoveOrgan(body, head), Is.True);
        Assert.That(_body.GetOrgan(body, "Brain"), Is.Null);
        Assert.That(SEntMan.HasComponent<DebrainedComponent>(body), Is.True);
        Assert.That(SComp<BrainComponent>(brain).Active, Is.False);
        Assert.That(_parts.GetSeveredOrgansContainer(head).ContainedEntities, Does.Contain(brain));

        Assert.That(_body.InsertOrgan(body, head), Is.True);
        Assert.That(_body.GetOrgan(body, "Brain"), Is.EqualTo(brain));
        Assert.That(SEntMan.HasComponent<DebrainedComponent>(body), Is.False);
        Assert.That(SEntMan.HasComponent<DelayedDeathComponent>(body), Is.False);
    }
}
