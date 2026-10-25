using System.Numerics;
using System.Linq;
using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.Server.Atmos.Components;
using Content.Server.Body.Components;
using Content.Shared._Trauma.Body;
using Content.Shared._Trauma.Common.Traumas;
using Content.Shared._Trauma.Common.Body;
using Content.Shared._Trauma.ItemSwitch;
using Content.Shared._Trauma.Surgery;
using Content.Shared._Trauma.Surgery.Steps;
using Content.Shared._Trauma.Surgery.Steps.Parts;
using Content.Shared._Trauma.Surgery.Tools;
using Content.Shared._Trauma.Traumas;
using Content.Shared._Trauma.Wounds;
using Content.Shared.Body;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Inventory;
using Content.Shared.Standing;
using Content.Shared.Temperature.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests._Trauma;

public sealed class SurgeryTest : GameTest
{
    [SidedDependency(Side.Server)] private BodySystem _body = default!;
    [SidedDependency(Side.Server)] private BodyPartSystem _parts = default!;
    [SidedDependency(Side.Server)] private SurgerySystem _surgery = default!;
    [SidedDependency(Side.Server)] private SharedHandsSystem _hands = default!;
    [SidedDependency(Side.Server)] private InventorySystem _inventory = default!;
    [SidedDependency(Side.Server)] private StandingStateSystem _standing = default!;
    [SidedDependency(Side.Server)] private TraumaSystem _trauma = default!;
    [SidedDependency(Side.Server)] private WoundSystem _wounds = default!;
    [SidedDependency(Side.Server)] private SharedItemSwitchSystem _switch = default!;

    private static readonly ProtoId<OrganCategoryPrototype> Arm = "ArmLeft";
    private static readonly ProtoId<OrganCategoryPrototype> Torso = "Torso";
    private static readonly ProtoId<OrganCategoryPrototype> Heart = "Heart";
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
                var tool = data.Tool!.Values.First().Component switch
                {
                    ScalpelComponent => SSpawn("Scalpel"),
                    RetractorComponent => SSpawn("Retractor"),
                    BoneSawComponent => SSpawn("Saw"),
                    HemostatComponent => SSpawn("Hemostat"),
                    BoneGelComponent => SSpawn("BoneGel"),
                    CauteryComponent => SSpawn("Cautery"),
                    StitchesComponent => SSpawn("MedicalStitches"),
                    TendingComponent => SSpawn("Hemostat"),
                    TweezersComponent => SSpawn("Hemostat"),
                    BoneSetterComponent => SSpawn("Bonesetter"),
                    InternalChildOrganComponent => organ!.Value,
                    _ => throw new InvalidOperationException($"Unexpected surgery tool in {stepId}"),
                };

                if (_hands.GetActiveItem(surgeon) is { } held)
                    Assert.That(_hands.TryDrop(surgeon, held), Is.True);
                Assert.That(_hands.TryPickupAnyHand(surgeon, tool), Is.True);
                Assert.That(_surgery.TryDoSurgeryStep(patient, part, surgeon, SComp<MetaDataComponent>(next.Surgery).EntityPrototype!.ID, stepId, out var error), Is.True, $"{procedure}/{stepId}: {error}");
                // Allow repeatable treatment to complete, including its final cancellation check.
                duration = data.Duration * 3 + 1;
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

    [Test]
    public async Task HeartCanBeRemovedAndReinsertedThroughSurgery()
    {
        var (surgeon, patient) = await SetupPatient();
        EntityUid torso = default;
        EntityUid heart = default;
        await Server.WaitAssertion(() =>
        {
            torso = _body.GetOrgan(patient, Torso)!.Value;
            heart = _body.GetOrgan(patient, Heart)!.Value;
        });

        await Operate(surgeon, patient, torso, "SurgeryRemoveHeart");
        await Server.WaitAssertion(() => Assert.That(_body.GetOrgan(patient, Heart), Is.Null));
        await Operate(surgeon, patient, torso, "SurgeryInsertHeart", heart);
        await Server.WaitAssertion(() =>
        {
            Assert.That(_body.GetOrgan(patient, Heart), Is.EqualTo(heart));
            Assert.That(_parts.GetOrgan(torso, Heart), Is.EqualTo(heart));
            Assert.That(SEntMan.HasComponent<EnabledOrganComponent>(heart), Is.True);
        });
    }

    [Test]
    [RunOnSide(Side.Server)]
    public void AdvancedToolSwitchesItsSurgicalCapabilities()
    {
        var tool = SSpawn("AdvancedBoneGel");
        Assert.That(SEntMan.HasComponent<BoneGelComponent>(tool), Is.True);
        Assert.That(_switch.Switch(tool, "bonesetter"), Is.True);
        Assert.That(SEntMan.HasComponent<BoneGelComponent>(tool), Is.False);
        Assert.That(SEntMan.HasComponent<BoneSetterComponent>(tool), Is.True);
        Assert.That(_switch.Switch(tool, "bonegel"), Is.True);
        Assert.That(SEntMan.HasComponent<BoneGelComponent>(tool), Is.True);
        Assert.That(SEntMan.HasComponent<BoneSetterComponent>(tool), Is.False);
    }
}
