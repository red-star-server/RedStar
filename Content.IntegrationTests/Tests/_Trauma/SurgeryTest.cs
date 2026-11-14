using System.Numerics;
using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.Server.Atmos.Components;
using Content.Server.Body.Components;
using Content.Shared._Trauma.Body;
using Content.Shared._Trauma.Body.Cavity;
using Content.Shared._Trauma.Common.Body;
using Content.Shared._Trauma.Common.Traumas;
using Content.Shared._Trauma.Surgery;
using Content.Shared._Trauma.Surgery.Graph;
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
    [SidedDependency(Side.Server)] private SurgeryGraphSystem _graph = default!;
    [SidedDependency(Side.Server)] private SharedHandsSystem _hands = default!;
    [SidedDependency(Side.Server)] private InventorySystem _inventory = default!;
    [SidedDependency(Side.Server)] private StandingStateSystem _standing = default!;
    [SidedDependency(Side.Server)] private TraumaSystem _trauma = default!;
    [SidedDependency(Side.Server)] private WoundSystem _wounds = default!;
    [SidedDependency(Side.Server)] private SharedToolSystem _tools = default!;

    private static readonly ProtoId<OrganCategoryPrototype> Arm = "ArmLeft";
    private static readonly ProtoId<OrganCategoryPrototype> Torso = "Torso";
    private static readonly ProtoId<ToolQualityPrototype> BoneHealing = "BoneHealing";
    private static readonly ProtoId<ToolQualityPrototype> BoneSetting = "BoneSetting";
    private static readonly EntProtoId BluntWound = "WoundBlunt";
    private static readonly ProtoId<SurgeryStagePrototype> Closed = "Closed";

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

    private EntityUid GetPartByType(EntityUid body, BodyPartType partType, BodyPartSymmetry? symmetry = null)
    {
        var found = _parts.FindBodyPart(body, partType, symmetry);
        Assert.That(found, Is.Not.Null, $"No {partType} {(symmetry?.ToString() ?? "")} part found");
        return found!.Value.Owner;
    }

    private async Task Operate(EntityUid surgeon, EntityUid part,
        ProtoId<SurgeryProcedurePrototype> procedure,
        ProtoId<OrganCategoryPrototype>? organ = null,
        EntityUid? organItem = null)
    {
        var instance = new SurgeryProcedureInstance(procedure, organ);
        var proc = Server.ProtoMan.Index(procedure);

        for (var i = 0; i < proc.Steps.Count; i++)
        {
            var step = proc.Steps[i];
            var index = i;
            var duration = 0f;
            await Server.WaitAssertion(() =>
            {
                var tool = ToolFor(step, organItem, procedure, index);
                if (_hands.GetActiveItem(surgeon) is { } held)
                    Assert.That(_hands.TryDrop(surgeon, held), Is.True);
                Assert.That(_hands.TryPickupAnyHand(surgeon, tool), Is.True,
                    $"{procedure}[{index}]: could not pick up {SEntMan.ToPrettyString(tool)}");

                Assert.That(_graph.CanPerformStep(part, surgeon, instance, index, out var popup), Is.True,
                    $"{procedure}[{index}]: {popup}");
                Assert.That(_graph.TryPerformStep(part, surgeon, instance, index), Is.True,
                    $"{procedure}[{index}]: step refused");

                duration = (float) step.Duration.TotalSeconds * (step.Repeatable ? 8 : 1) + 1;
            });
            await RunSeconds(duration);
        }

        await Server.WaitAssertion(() =>
        {
            var comp = SComp<OperatedComponent>(part);
            Assert.That(comp.ActiveProcedure, Is.Null, $"{procedure} did not finish");
        });
    }

    private EntityUid ToolFor(SurgeryStepEntry step, EntityUid? organItem, string procedure, int index)
    {
        if (step.ProcedureOrgan || step.Organ is not null)
        {
            Assert.That(organItem, Is.Not.Null, $"{procedure}[{index}]: step needs an organ in hand");
            return organItem!.Value;
        }

        if (step.AnyItem)
            return SSpawn("Pen");

        Assert.That(step.Quality, Is.Not.Null, $"{procedure}[{index}]: step declares no tool");
        return SSpawn(step.Quality.Value.Id switch
        {
            "Drilling" => "Drill",
            "Slicing" => "Scalpel",
            "Retracting" => "Retractor",
            "Sawing" => "Saw",
            "Clamping" => "Hemostat",
            "BoneHealing" => "BoneGel",
            "Cauterizing" => "Cautery",
            "Suturing" => "MedicalStitches",
            "Tending" => "Hemostat",
            "Manipulating" => "Hemostat",
            "BoneSetting" => "Bonesetter",
            var quality => throw new InvalidOperationException($"Unexpected surgery quality {quality} in {procedure}[{index}]"),
        });
    }

    [Test]
    public async Task BoneTreatmentCanBeCompletedAndIncisionClosed()
    {
        var (surgeon, patient) = await SetupPatient();
        EntityUid armPart = default;
        await Server.WaitAssertion(() =>
        {
            armPart = GetPartByType(patient, BodyPartType.Arm, BodyPartSymmetry.Left);
            Assert.That(_wounds.TryInduceWound(armPart, BluntWound, 5, out var wound), Is.True);
            Assert.That(_trauma.ApplyBoneTrauma(armPart, (wound!.Value.Owner, SComp<TraumaInflicterComponent>(wound.Value.Owner)), 34), Is.True);
        });

        await Operate(surgeon, armPart, "OpenIncision");
        await Operate(surgeon, armPart, "MendBones");
        await Operate(surgeon, armPart, "CloseIncision");

        await Server.WaitAssertion(() =>
        {
            var bone = SComp<BoneComponent>(armPart);
            Assert.That(bone.BoneIntegrity, Is.EqualTo(bone.IntegrityCap));
            Assert.That(_trauma.HasWoundableTrauma(armPart, TraumaType.BoneDamage), Is.False);
            Assert.That(_graph.GetStage(armPart), Is.EqualTo(Closed));
        });
    }

    [TestCase("Heart")]
    [TestCase("Appendix")]
    public async Task OrganCanBeRemovedAndReinsertedThroughSurgery(string organCategory)
    {
        var (surgeon, patient) = await SetupPatient();
        ProtoId<OrganCategoryPrototype> category = organCategory;
        EntityUid torsoPart = default;
        EntityUid organ = default;
        await Server.WaitAssertion(() =>
        {
            torsoPart = GetPartByType(patient, BodyPartType.Torso);
            organ = _body.GetOrgan(patient, category)!.Value;
        });

        await Operate(surgeon, torsoPart, "OpenIncision");
        await Operate(surgeon, torsoPart, "OpenRibcage");
        await Operate(surgeon, torsoPart, "RemoveOrgan", category);
        await Server.WaitAssertion(() => Assert.That(_body.GetOrgan(patient, category), Is.Null));

        await Operate(surgeon, torsoPart, "InsertOrgan", category, organ);
        await Server.WaitAssertion(() =>
        {
            Assert.That(_body.GetOrgan(patient, category), Is.EqualTo(organ));
            Assert.That(_parts.GetOrgan(torsoPart, category), Is.EqualTo(organ));
            Assert.That(SEntMan.HasComponent<EnabledOrganComponent>(organ), Is.True);
        });
    }

    [Test]
    [RunOnSide(Side.Server)]
    public void MultipleToolCyclesItsSurgicalCapabilities()
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
    public async Task DetachedPartCancelsSurgeryAndCanBeReattached()
    {
        var (surgeon, patient) = await SetupPatient();
        EntityUid armPart = default;
        EntityUid torsoPart = default;
        await Server.WaitAssertion(() =>
        {
            armPart = GetPartByType(patient, BodyPartType.Arm, BodyPartSymmetry.Left);
            Assert.That(_hands.TryPickupAnyHand(surgeon, SSpawn("Scalpel")), Is.True);
            Assert.That(_graph.TryPerformStep(armPart, surgeon, new SurgeryProcedureInstance("OpenIncision", null), 0), Is.True);
            Assert.That(_body.RemoveOrgan(patient, armPart), Is.True);
        });
        await RunSeconds(8);
        await Server.WaitAssertion(() => Assert.That(_graph.GetStage(armPart), Is.EqualTo(Closed)));

        await Server.WaitAssertion(() => torsoPart = GetPartByType(patient, BodyPartType.Torso));

        await Operate(surgeon, torsoPart, "OpenIncision");
        await Operate(surgeon, torsoPart, "AttachPart", Arm, armPart);
        await Server.WaitAssertion(() =>
        {
            Assert.That(_body.GetOrgan(patient, Arm), Is.EqualTo(armPart));
            Assert.That(SEntMan.HasComponent<OrganReattachedComponent>(armPart), Is.False);
        });
    }

    [Test]
    public async Task ClothingOverThePartBlocksSurgery()
    {
        var (surgeon, patient) = await SetupPatient();
        await Server.WaitAssertion(() =>
        {
            var torsoPart = GetPartByType(patient, BodyPartType.Torso);
            Assert.That(_inventory.TryEquip(patient, SSpawn("ClothingUniformJumpsuitColorGrey"), "innerclothing", force: true), Is.True);
            Assert.That(_hands.TryPickupAnyHand(surgeon, SSpawn("Scalpel")), Is.True);
            Assert.That(_graph.CanPerformStep(torsoPart, surgeon, new SurgeryProcedureInstance("OpenIncision", null), 0, out _), Is.False);
        });
    }

    [Test]
    public async Task CavityInsertionUsesTheActiveHand()
    {
        var (surgeon, patient) = await SetupPatient();
        EntityUid torsoPart = default;
        EntityUid selected = default;
        await Server.WaitAssertion(() => torsoPart = GetPartByType(patient, BodyPartType.Torso));

        await Operate(surgeon, torsoPart, "OpenIncision");
        await Operate(surgeon, torsoPart, "OpenRibcage");

        await Server.WaitAssertion(() =>
        {
            selected = SSpawn("Pen");
            Assert.That(_hands.TryGetEmptyHand(surgeon, out var hand), Is.True);
            Assert.That(_hands.TryPickupAnyHand(surgeon, selected), Is.True);
            Assert.That(_hands.TrySetActiveHand(surgeon, hand), Is.True);
            Assert.That(_graph.TryPerformStep(torsoPart, surgeon, new SurgeryProcedureInstance("InsertCavityItem", null), 0), Is.True);
        });
        await RunSeconds(14);
        await Server.WaitAssertion(() =>
        {
            var containers = Server.System<SharedContainerSystem>();
            Assert.That(containers.TryGetContainer(torsoPart, "_part_cavity", out var cavity), Is.True);
            Assert.That(cavity!.ContainedEntities, Does.Contain(selected));
            Assert.That(_graph.TryPerformStep(torsoPart, surgeon, new SurgeryProcedureInstance("RemoveCavityItem", null), 0), Is.True);
        });
        await RunSeconds(14);
        await Server.WaitAssertion(() =>
        {
            Assert.That(_hands.GetActiveItem(surgeon), Is.EqualTo(selected));
            Assert.That(_hands.TryDrop(surgeon, selected), Is.True);
            var containers = Server.System<SharedContainerSystem>();
            Assert.That(containers.TryGetContainer(torsoPart, "_part_cavity", out var cavity), Is.True);
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
        await RunSeconds(20);
        await Server.WaitAssertion(() =>
        {
            Assert.That(SEntMan.HasComponent<InsideBodyPartComponent>(selected), Is.False);
            Assert.That(_trauma.HasWoundableTrauma(torsoPart, TraumaType.BoneDamage), Is.True);
        });
    }
}
