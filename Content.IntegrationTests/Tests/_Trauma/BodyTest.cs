using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.IntegrationTests.Tests.Helpers;
using Content.Shared._Trauma.Body;
using Content.Shared._Trauma.Common.Body;
using Content.Shared._Trauma.DelayedDeath;
using Content.Shared.Body;
using Content.Shared.Body.Components;
using Content.Shared.Eye.Blinding.Components;
using Content.Shared.Atmos.Components;
using Content.Shared.Atmos.Rotting;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Movement.Components;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests._Trauma;

[TestFixture]
[TestOf(typeof(BodyPartSystem))]
public sealed class BodyTest : GameTest
{
    private sealed class OrganRemovalListenerSystem : TestListenerSystem<OrganRemovedFromPartEvent>;

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

- type: entity
  id: TraumaTestAlternativeParentBody
  parent: TraumaTestBody
  components:
  - type: InitialBody
    organs: !Clear
      Torso: TraumaTestTorso
      Brain: TraumaTestAlternativeParentBrain
      Head: TraumaTestHead

- type: entity
  id: TraumaTestAlternativeParentBrain
  parent: TraumaTestBrain
  components:
  - type: ChildOrgan
    parents: [Torso, Head]

- type: entity
  id: TraumaTestZeroLegRequirement
  parent: MobHuman
  components:
  - type: Legs
    required: 0
";

    [SidedDependency(Side.Server)] private BodySystem _body = default!;
    [SidedDependency(Side.Server)] private BodyPartSystem _parts = default!;
    [SidedDependency(Side.Server)] private BodyRestoreSystem _restore = default!;
    [SidedDependency(Side.Server)] private SharedGodmodeSystem _godmode = default!;
    private static readonly ProtoId<OrganCategoryPrototype> HandLeftCategory = "HandLeft";

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
            if (SEntMan.TryGetComponent<BodyPartComponent>(organ, out var part))
                Assert.That(BodySystem.PartTypeOrgans[part.PartType], Does.Contain(category), category.ToString());

            if (!SEntMan.TryGetComponent<ChildOrganComponent>(organ, out var child))
                continue;

            Assert.That(child.Parent, Is.Not.Null, category.ToString());
            Assert.That(_parts.GetOrgan(child.Parent.Value, category), Is.EqualTo(organ));
        }
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
    public void DisabledEyesBlindHumanUntilEnabled()
    {
        var body = SSpawn("MobHuman");
        var eyes = _body.GetOrgan(body, "Eyes").Value;
        Assert.That(_body.DisableOrgan(eyes), Is.True);
        Assert.That(SComp<BlindableComponent>(body).IsBlind, Is.True);
        Assert.That(_body.GetOrgan(body, "Eyes"), Is.EqualTo(eyes));

        Assert.That(_body.EnableOrgan(eyes), Is.True);
        Assert.That(SComp<BlindableComponent>(body).IsBlind, Is.False);
    }

    [Test]
    [RunOnSide(Side.Server)]
    public void InitialBodySelectsParentWithCompatibleSlot()
    {
        var body = SSpawn("TraumaTestAlternativeParentBody");
        var brain = _body.GetOrgan(body, "Brain").Value;
        var head = _body.GetOrgan(body, "Head").Value;
        Assert.That(SComp<ChildOrganComponent>(brain).Parent, Is.EqualTo(head));
        Assert.That(SComp<BodyPartComponent>(head).Children.Values, Does.Contain(brain));
    }

    [Test]
    [RunOnSide(Side.Server)]
    public void DetachedOrgansCanRot()
    {
        var body = SSpawn("TraumaTestBody");
        var hand = _body.GetOrgan(body, "HandLeft").Value;
        var attached = new IsRottingEvent();
        SEntMan.EventBus.RaiseLocalEvent(hand, ref attached);
        Assert.That(attached.Handled, Is.True);

        Assert.That(_body.RemoveOrgan(body, hand), Is.True);
        var detached = new IsRottingEvent();
        SEntMan.EventBus.RaiseLocalEvent(hand, ref detached);
        Assert.That(detached.Handled, Is.False);
    }

    [Test]
    public async Task DeletingRootPartQueuesBodyDeletion()
    {
        var map = await Pair.CreateTestMap();
        await Server.WaitAssertion(() =>
        {
            var body = SSpawnAtPosition("TraumaTestBody", map.GridCoords);
            var torso = _body.GetOrgan(body, "Torso").Value;
            SEntMan.EnsureComponent<UnremoveableOrganComponent>(torso);
            SEntMan.DeleteEntity(torso);
            Assert.That(SEntMan.IsQueuedForDeletion(body), Is.True);
        });
    }

    [Test]
    [RunOnSide(Side.Server)]
    public void ZeroRequiredLegsKeepsMovementFinite()
    {
        var body = SSpawn("TraumaTestZeroLegRequirement");
        var leg = _body.GetOrgan(body, "LegLeft").Value;
        Assert.That(_body.RemoveOrgan(body, leg), Is.True);
        var movement = SComp<MovementSpeedModifierComponent>(body);
        Assert.That(float.IsFinite(movement.BaseWalkSpeed), Is.True);
        Assert.That(float.IsFinite(movement.BaseSprintSpeed), Is.True);
        Assert.That(float.IsFinite(movement.BaseAcceleration), Is.True);
    }

    [Test]
    [RunOnSide(Side.Server)]
    public void GodmodePropagatesToOrgansInsertedLater()
    {
        var body = SSpawn(null);
        SEntMan.EnsureComponent<BodyComponent>(body);
        _godmode.EnableGodmode(body);
        var torso = SSpawn("TraumaTestTorso");
        var pressure = SEntMan.EnsureComponent<MovedByPressureComponent>(torso);
        Assert.That(pressure.Enabled, Is.True);

        Assert.That(_body.InsertOrgan(body, torso), Is.True);
        Assert.That(SEntMan.HasComponent<GodmodeComponent>(torso), Is.True);
        Assert.That(pressure.Enabled, Is.False);
        _godmode.DisableGodmode(body);
        Assert.That(SEntMan.HasComponent<GodmodeComponent>(torso), Is.False);
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
        Assert.That(_parts.GetOrgan(arm, HandLeftCategory), Is.EqualTo(hand));
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
    public void RemovingSlotPreservesOrganWhenRemovalIsBlocked()
    {
        var body = SSpawn("TraumaTestBody");
        var arm = _body.GetOrgan(body, "ArmLeft").Value;
        var hand = _body.GetOrgan(body, "HandLeft").Value;
        var part = SComp<BodyPartComponent>(arm);
        var listener = SEntMan.EnsureComponent<TestListenerComponent>(arm);
        SEntMan.EnsureComponent<UnremoveableOrganComponent>(hand);

        Assert.That(_parts.TryRemoveSlot(arm, HandLeftCategory), Is.False);
        Assert.That(part.Slots, Does.Contain(HandLeftCategory));
        Assert.That(part.Children[HandLeftCategory], Is.EqualTo(hand));
        Assert.That(_body.GetOrgan(body, "HandLeft"), Is.EqualTo(hand));
        Assert.That(SComp<ChildOrganComponent>(hand).Parent, Is.EqualTo(arm));
        Assert.That(listener.Events.ContainsKey(typeof(OrganRemovedFromPartEvent)), Is.False);
    }

    [Test]
    [RunOnSide(Side.Server)]
    public void RemovingOccupiedSlotRaisesOneRemovalEvent()
    {
        var body = SSpawn("TraumaTestBody");
        var arm = _body.GetOrgan(body, "ArmLeft").Value;
        var hand = _body.GetOrgan(body, "HandLeft").Value;
        var part = SComp<BodyPartComponent>(arm);
        var listener = SEntMan.EnsureComponent<TestListenerComponent>(arm);

        Assert.That(_parts.TryRemoveSlot(arm, HandLeftCategory), Is.True);
        Assert.That(part.Slots, Does.Not.Contain(HandLeftCategory));
        Assert.That(part.Children.ContainsKey(HandLeftCategory), Is.False);
        Assert.That(_body.GetOrgan(body, "HandLeft"), Is.Null);
        Assert.That(SComp<ChildOrganComponent>(hand).Parent, Is.Null);
        Assert.That(listener.Events[typeof(OrganRemovedFromPartEvent)], Has.Count.EqualTo(1));
        Assert.That(_parts.TryRemoveSlot(arm, HandLeftCategory), Is.False);
        Assert.That(listener.Events[typeof(OrganRemovedFromPartEvent)], Has.Count.EqualTo(1));
    }

    [TestCase(false)]
    [TestCase(true)]
    [RunOnSide(Side.Server)]
    public void RemovingSlotClearsChildWhenBodyRemovalRaisesNoEvent(bool severed)
    {
        var body = SSpawn("TraumaTestBody");
        var arm = _body.GetOrgan(body, "ArmLeft").Value;
        var hand = _body.GetOrgan(body, "HandLeft").Value;
        if (severed)
            Assert.That(_body.RemoveOrgan(body, arm), Is.True);
        else
            Assert.That(_body.RemoveOrgan(body, hand), Is.True);

        var part = SComp<BodyPartComponent>(arm);
        // The attached case reproduces a stale index after the organ has already left the body.
        // The severed case retains its hand, but has no body to raise the removal event.
        part.Children[HandLeftCategory] = hand;
        var listener = SEntMan.EnsureComponent<TestListenerComponent>(arm);

        Assert.That(_parts.TryRemoveSlot(arm, HandLeftCategory), Is.True);
        Assert.That(part.Slots, Does.Not.Contain(HandLeftCategory));
        Assert.That(part.Children.ContainsKey(HandLeftCategory), Is.False);
        Assert.That(listener.Events[typeof(OrganRemovedFromPartEvent)], Has.Count.EqualTo(1));
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
        Assert.That(_parts.GetOrgan(restoredArm, HandLeftCategory), Is.EqualTo(restoredHand));
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
