using System.Linq;
using System.Numerics;
using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.Server.Atmos.Components;
using Content.Server.Body.Components;
using Content.Shared._Trauma.Body;
using Content.Shared._Trauma.Body.Cavity;
using Content.Shared._Trauma.Common.Body;
using Content.Shared._Trauma.Common.Traumas;
using Content.Shared._Trauma.Speech;
using Content.Shared._Trauma.Surgery;
using Content.Shared._Trauma.Surgery.Steps;
using Content.Shared._Trauma.Surgery.Steps.Parts;
using Content.Shared._Trauma.Traumas;
using Content.Shared._Trauma.Wounds;
using Content.Shared.Body;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Inventory;
using Content.Shared.Standing;
using Content.Shared.Tools;
using Content.Shared.Tools.Systems;
using Content.Shared.Temperature.Components;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests._Trauma;

public sealed partial class SurgeryTest : GameTest
{
    [SidedDependency(Side.Server)] private BodySystem _body = default!;
    [SidedDependency(Side.Server)] private BodyPartSystem _parts = default!;
    [SidedDependency(Side.Server)] private SurgerySystem _surgery = default!;
    [SidedDependency(Side.Server)] private SharedHandsSystem _hands = default!;
    [SidedDependency(Side.Server)] private InventorySystem _inventory = default!;
    [SidedDependency(Side.Server)] private StandingStateSystem _standing = default!;
    [SidedDependency(Side.Server)] private TraumaSystem _trauma = default!;
    [SidedDependency(Side.Server)] private WoundSystem _wounds = default!;
    [SidedDependency(Side.Server)] private SharedToolSystem _tools = default!;

    private static readonly ProtoId<OrganCategoryPrototype> Arm = "ArmLeft";
    private static readonly ProtoId<OrganCategoryPrototype> Torso = "Torso";
    private static readonly ProtoId<OrganCategoryPrototype> Head = "Head";
    private static readonly ProtoId<ToolQualityPrototype> BoneHealing = "BoneHealing";
    private static readonly ProtoId<ToolQualityPrototype> BoneSetting = "BoneSetting";
    private static readonly EntProtoId BluntWound = "WoundBlunt";

    private async Task<(EntityUid Surgeon, EntityUid Patient)> SetupPatient()
    {
        var map = await Pair.CreateTestMap();
        EntityUid surgeon = default;
        EntityUid patient = default;
        await Server.WaitAssertion(() =>
        {
            surgeon = SSpawnAtPosition("MobHuman", map.GridCoords);
            patient = SSpawnAtPosition("MobHuman", map.GridCoords.Offset(new Vector2(1, 0)));
            foreach (var mob in new[] { surgeon, patient })
            {
                // Keep the test about surgery, without unrelated vacuum and temperature damage.
                SEntMan.RemoveComponent<BarotraumaComponent>(mob);
                SEntMan.RemoveComponent<RespiratorComponent>(mob);
                SEntMan.RemoveComponent<TemperatureDamageComponent>(mob);
            }

            Assert.That(_inventory.TryEquip(surgeon, SSpawn("ClothingHandsGlovesLatex"), "gloves", force: true), Is.True);
            Assert.That(_inventory.TryEquip(surgeon, SSpawn("ClothingMaskSterile"), "mask", force: true), Is.True);
            _standing.Down(patient);
        });
        return (surgeon, patient);
    }

    private async Task Operate(EntityUid surgeon, EntityUid patient, EntityUid part, EntProtoId procedure, EntityUid? organ = null)
    {
        for (var i = 0; i < 20; i++)
        {
            var finished = false;
            var duration = 0f;
            await Server.WaitAssertion(() =>
            {
                var surgery = _surgery.GetSingleton(procedure)!.Value;
                if (_surgery.GetNextStep(patient, part, surgery, surgeon) is not { } next)
                {
                    finished = true;
                    return;
                }

                var stepId = next.Surgery.Comp.Steps[next.Step];
                var step = _surgery.GetSingleton(stepId)!.Value;
                var data = SComp<SurgeryStepComponent>(step);
                var tool = data.ToolQualities is { Count: > 0 } qualities
                    ? qualities.First().ToString() switch
                    {
                        "Drilling" => SSpawn("Drill"),
                        "Slicing" => SSpawn("Scalpel"),
                        "Retracting" => SSpawn("Retractor"),
                        "Sawing" => SSpawn("Saw"),
                        "Clamping" => SSpawn("Hemostat"),
                        "BoneHealing" => SSpawn("BoneGel"),
                        "Cauterizing" => SSpawn("Cautery"),
                        "Suturing" => SSpawn("MedicalStitches"),
                        "Tending" => SSpawn("Hemostat"),
                        "Manipulating" => SSpawn("Hemostat"),
                        "BoneSetting" => SSpawn("Bonesetter"),
                        var quality => throw new InvalidOperationException($"Unexpected surgery quality {quality} in {stepId}"),
                    }
                    : data.RequiredItemComponent switch
                    {
                        nameof(InternalChildOrganComponent) => organ!.Value,
                        nameof(BodyPartComponent) => organ!.Value,
                        var component => throw new InvalidOperationException($"Unexpected surgery tool {component} in {stepId}"),
                    };

                if (_hands.GetActiveItem(surgeon) is { } held)
                    Assert.That(_hands.TryDrop(surgeon, held), Is.True);
                Assert.That(_hands.TryPickupAnyHand(surgeon, tool), Is.True,
                    $"{procedure}/{stepId}: could not pick up {SEntMan.ToPrettyString(tool)}");
                Assert.That(_surgery.TryDoSurgeryStep(patient, part, surgeon, SComp<MetaDataComponent>(next.Surgery).EntityPrototype!.ID, stepId, out var error), Is.True, $"{procedure}/{stepId}: {error}");
                // Allow repeatable treatment to complete, including its final cancellation check.
                duration = (float) data.Duration.TotalSeconds * 3 + 1;
            });

            if (finished)
                return;
            await RunSeconds(duration);
        }

        Assert.Fail($"Surgery {procedure} did not finish");
    }

    [Test]
    public async Task BoneTreatmentCanBeCompletedAndIncisionClosed()
    {
        var (surgeon, patient) = await SetupPatient();
        EntityUid arm = default;
        await Server.WaitAssertion(() =>
        {
            arm = _body.GetOrgan(patient, Arm)!.Value;
            Assert.That(_wounds.TryInduceWound(arm, BluntWound, 5, out var wound), Is.True);
            Assert.That(_trauma.ApplyBoneTrauma(arm, (wound!.Value.Owner, SComp<TraumaInflicterComponent>(wound.Value.Owner)), 34), Is.True);
        });

        await Operate(surgeon, patient, arm, "SurgeryOpenIncision");
        await Operate(surgeon, patient, arm, "SurgeryMendBones");
        await Operate(surgeon, patient, arm, "SurgeryCloseIncision");
        await Server.WaitAssertion(() =>
        {
            var bone = SComp<BoneComponent>(arm);
            Assert.That(bone.BoneIntegrity, Is.EqualTo(bone.IntegrityCap));
            Assert.That(_trauma.HasWoundableTrauma(arm, TraumaType.BoneDamage), Is.False);
            Assert.That(SEntMan.HasComponent<IncisionOpenComponent>(arm), Is.False);
            Assert.That(SEntMan.HasComponent<BonesSawedComponent>(arm), Is.False);
        });
    }

    [TestCase("Heart", "SurgeryRemoveHeart", "SurgeryInsertHeart")]
    [TestCase("Appendix", "SurgeryRemoveAppendix", "SurgeryInsertAppendix")]
    public async Task OrganCanBeRemovedAndReinsertedThroughSurgery(string organCategory, string removal, string insertion)
    {
        var (surgeon, patient) = await SetupPatient();
        ProtoId<OrganCategoryPrototype> category = organCategory;
        EntityUid torso = default;
        EntityUid organ = default;
        await Server.WaitAssertion(() =>
        {
            torso = _body.GetOrgan(patient, Torso)!.Value;
            organ = _body.GetOrgan(patient, category)!.Value;
        });

        await Operate(surgeon, patient, torso, removal);
        await Server.WaitAssertion(() => Assert.That(_body.GetOrgan(patient, category), Is.Null));
        await Operate(surgeon, patient, torso, insertion, organ);
        await Server.WaitAssertion(() =>
        {
            Assert.That(_body.GetOrgan(patient, category), Is.EqualTo(organ));
            Assert.That(_parts.GetOrgan(torso, category), Is.EqualTo(organ));
            Assert.That(SEntMan.HasComponent<EnabledOrganComponent>(organ), Is.True);
        });
    }

    [Test]
    [RunOnSide(Side.Server)]
    public void AdvancedToolSwitchesItsSurgicalCapabilities()
    {
        var tool = SSpawn("AdvancedBoneGel");
        Assert.That(_tools.HasQuality(tool, BoneHealing), Is.True);
        Assert.That(_tools.HasQuality(tool, BoneSetting), Is.False);
        Assert.That(_tools.CycleMultipleTool(tool), Is.True);
        Assert.That(_tools.HasQuality(tool, BoneHealing), Is.False);
        Assert.That(_tools.HasQuality(tool, BoneSetting), Is.True);
        Assert.That(_tools.CycleMultipleTool(tool), Is.True);
        Assert.That(_tools.HasQuality(tool, BoneHealing), Is.True);
        Assert.That(_tools.HasQuality(tool, BoneSetting), Is.False);
    }
    [Test]
    public async Task LobotomyCanBeReversed()
    {
        var (surgeon, patient) = await SetupPatient();
        EntityUid head = default;
        await Server.WaitAssertion(() => head = _body.GetOrgan(patient, Head)!.Value);
        await Operate(surgeon, patient, head, "SurgeryLobotomize");
        await Server.WaitAssertion(() => Assert.That(SEntMan.HasComponent<OhioAccentComponent>(patient), Is.True));
        await Operate(surgeon, patient, head, "SurgeryMendBrainTissue");
        await Server.WaitAssertion(() => Assert.That(SEntMan.HasComponent<OhioAccentComponent>(patient), Is.False));
    }

    [Test]
    public async Task DetachedPartCancelsSurgeryAndCanBeReattached()
    {
        var (surgeon, patient) = await SetupPatient();
        EntityUid arm = default;
        await Server.WaitAssertion(() =>
        {
            arm = _body.GetOrgan(patient, Arm)!.Value;
            Assert.That(_surgery.GetSingleton("MobHuman"), Is.Null);
            Assert.That(_hands.TryPickupAnyHand(surgeon, SSpawn("Scalpel")), Is.True);
            Assert.That(_surgery.TryDoSurgeryStep(patient, arm, surgeon, "SurgeryOpenIncision", "SurgeryStepOpenIncisionScalpel", out _), Is.True);
            Assert.That(_body.RemoveOrgan(patient, arm), Is.True);
        });
        await RunSeconds(8);
        await Server.WaitAssertion(() => Assert.That(SEntMan.HasComponent<IncisionOpenComponent>(arm), Is.False));
        EntityUid torso = default;
        await Server.WaitAssertion(() => torso = _body.GetOrgan(patient, Torso)!.Value);
        await Operate(surgeon, patient, torso, "SurgeryAttachLeftArm", arm);
        await Server.WaitAssertion(() =>
        {
            Assert.That(_body.GetOrgan(patient, Arm), Is.EqualTo(arm));
            Assert.That(SEntMan.HasComponent<OrganReattachedComponent>(arm), Is.False);
        });
    }

    [Test]
    public async Task CavityInsertionUsesTheActiveHand()
    {
        var (surgeon, patient) = await SetupPatient();
        EntityUid torso = default;
        EntityUid selected = default;
        await Server.WaitAssertion(() => torso = _body.GetOrgan(patient, Torso)!.Value);
        await Operate(surgeon, patient, torso, "SurgeryOpenRibcage");
        await Server.WaitAssertion(() =>
        {
            // Leave the last tool in the first hand and select an item in the other hand.
            selected = SSpawn("Pen");
            Assert.That(_hands.TryGetEmptyHand(surgeon, out var hand), Is.True);
            Assert.That(_hands.TryPickupAnyHand(surgeon, selected), Is.True);
            Assert.That(_hands.TrySetActiveHand(surgeon, hand), Is.True);
            Assert.That(_surgery.TryDoSurgeryStep(patient, torso, surgeon, "SurgeryInsertItem", "SurgeryStepInsertItem", out _), Is.True);
        });
        await RunSeconds(14);
        await Server.WaitAssertion(() =>
        {
            var containers = Server.System<SharedContainerSystem>();
            Assert.That(containers.TryGetContainer(torso, "_part_cavity", out var cavity), Is.True);
            Assert.That(cavity!.ContainedEntities, Does.Contain(selected));
            Assert.That(_surgery.TryDoSurgeryStep(patient, torso, surgeon, "SurgeryInsertItem", "SurgeryStepRemoveItem", out _), Is.True);
        });
        await RunSeconds(14);
        await Server.WaitAssertion(() =>
        {
            Assert.That(_hands.GetActiveItem(surgeon), Is.EqualTo(selected));
            Assert.That(_hands.TryDrop(surgeon, selected), Is.True);
            var containers = Server.System<SharedContainerSystem>();
            Assert.That(containers.TryGetContainer(torso, "_part_cavity", out var cavity), Is.True);
            var mouse = SSpawn("MobMouse");
            SEntMan.RemoveComponent<BarotraumaComponent>(mouse);
            SEntMan.RemoveComponent<RespiratorComponent>(mouse);
            SEntMan.RemoveComponent<TemperatureDamageComponent>(mouse);
            Assert.That(containers.Insert(mouse, cavity), Is.True);
            selected = mouse;
            var burst = new BodyPartBurstEvent();
            SEntMan.EventBus.RaiseLocalEvent(mouse, burst);
            Assert.That(burst.Handled, Is.True);
        });
        // Let the burst and its temporary paralysis finish before fixture cleanup.
        await RunSeconds(20);
        await Server.WaitAssertion(() =>
        {
            Assert.That(SEntMan.HasComponent<InsideBodyPartComponent>(selected), Is.False);
            Assert.That(_trauma.HasWoundableTrauma(torso, TraumaType.BoneDamage), Is.True);
        });
    }

}
