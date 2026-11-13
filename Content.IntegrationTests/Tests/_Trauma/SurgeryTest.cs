using System.Numerics;
using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.Server.Atmos.Components;
using Content.Server.Body.Components;
using Content.Shared._Trauma.Body;
using Content.Shared._Trauma.Body.Cavity;
using Content.Shared._Trauma.Common.Body;
using Content.Shared._Trauma.Common.Traumas;
using Content.Shared._Trauma.Surgery.Graph;
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
    [SidedDependency(Side.Server)] private SurgeryGraphSystem _graph = default!;
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

    /// <summary>
    /// Drives a single graph edge to completion, picking the tool each step asks for.
    /// Self-looping treatment edges (StopBlood, MendBones) repeat until their conditions clear.
    /// </summary>
    private async Task OperateEdge(EntityUid surgeon, EntityUid part, ProtoId<SurgeryGraphPrototype> graphId, string edgeId, EntityUid? organ = null)
    {
        await Server.WaitAssertion(() =>
        {
            if (!SEntMan.TryGetComponent<OperatedComponent>(part, out var operated) || operated.GraphId == null)
                Assert.That(_graph.TryAssignGraph(part, graphId), Is.True);
        });

        for (var i = 0; i < 30; i++)
        {
            var finished = false;
            var duration = 0f;
            await Server.WaitAssertion(() =>
            {
                var comp = SComp<OperatedComponent>(part);
                if (!_graph.TryGetGraph(comp, out var graph)
                    || _graph.GetEdge(graph, comp.CurrentNode, edgeId) is not { } edge
                    || !_graph.EdgeConditionsMet(part, edge))
                {
                    finished = true;
                    return;
                }

                var index = comp.ActiveEdgeId == edgeId ? comp.CompletedSteps : 0;
                var step = edge.Steps[index];
                var tool = PickStepTool(step, organ);

                if (!_hands.IsHolding(surgeon, tool))
                {
                    if (_hands.GetActiveItem(surgeon) is { } held)
                        Assert.That(_hands.TryDrop(surgeon, held), Is.True);
                    Assert.That(_hands.TryPickupAnyHand(surgeon, tool), Is.True,
                        $"{edgeId}: could not pick up {SEntMan.ToPrettyString(tool)}");
                }

                Assert.That(_graph.TryPerformEdgeStep(part, surgeon, tool, edgeId), Is.True,
                    $"{edgeId}: step {index} did not start");
                duration = (float) step.Duration.TotalSeconds * 3 + 1;
            });

            if (finished)
                return;
            await RunSeconds(duration);
        }

        Assert.Fail($"Surgery edge {edgeId} did not finish");
    }

    private EntityUid PickStepTool(SurgeryStepEntry step, EntityUid? organ)
    {
        if (step.Organ != null)
            return organ!.Value;

        if (step.AnyItem)
            return organ ?? SSpawn("Pen");

        var quality = step.Quality!.Value.ToString();
        return quality switch
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
            _ => throw new InvalidOperationException($"Unexpected surgery quality {quality}"),
        };
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

        await OperateEdge(surgeon, arm, "SurgeryGraphLimb", "OpenIncision");
        await OperateEdge(surgeon, arm, "SurgeryGraphLimb", "StopBlood");
        await OperateEdge(surgeon, arm, "SurgeryGraphLimb", "MendBones");
        await OperateEdge(surgeon, arm, "SurgeryGraphLimb", "CloseIncision");
        await Server.WaitAssertion(() =>
        {
            var bone = SComp<BoneComponent>(arm);
            Assert.That(bone.BoneIntegrity, Is.EqualTo(bone.IntegrityCap));
            Assert.That(_trauma.HasWoundableTrauma(arm, TraumaType.BoneDamage), Is.False);
            Assert.That(SComp<OperatedComponent>(arm).CurrentNode, Is.EqualTo("Closed"));
        });
    }

    [TestCase("Heart", "RemoveHeart", "InsertHeart")]
    [TestCase("Appendix", "RemoveAppendix", "InsertAppendix")]
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

        await OperateEdge(surgeon, torso, "SurgeryGraphTorso", "OpenIncision");
        await OperateEdge(surgeon, torso, "SurgeryGraphTorso", "OpenBones");
        await OperateEdge(surgeon, torso, "SurgeryGraphTorso", removal);
        await Server.WaitAssertion(() => Assert.That(_body.GetOrgan(patient, category), Is.Null));
        await OperateEdge(surgeon, torso, "SurgeryGraphTorso", insertion, organ);
        await Server.WaitAssertion(() =>
        {
            Assert.That(_body.GetOrgan(patient, category), Is.EqualTo(organ));
            Assert.That(_parts.GetOrgan(torso, category), Is.EqualTo(organ));
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
        EntityUid arm = default;
        await Server.WaitAssertion(() =>
        {
            arm = _body.GetOrgan(patient, Arm)!.Value;
            Assert.That(_graph.TryAssignGraph(arm, "SurgeryGraphLimb"), Is.True);
            var scalpel = SSpawn("Scalpel");
            Assert.That(_hands.TryPickupAnyHand(surgeon, scalpel), Is.True);
            Assert.That(_graph.TryPerformEdgeStep(arm, surgeon, scalpel, "OpenIncision"), Is.True);
            Assert.That(_body.RemoveOrgan(patient, arm), Is.True);
        });
        await RunSeconds(8);
        await Server.WaitAssertion(() =>
        {
            var comp = SComp<OperatedComponent>(arm);
            Assert.That(comp.CurrentNode, Is.EqualTo("Closed"));
            Assert.That(SurgeryGraphSystem.IsBusy(comp), Is.False);
        });

        EntityUid torso = default;
        await Server.WaitAssertion(() => torso = _body.GetOrgan(patient, Torso)!.Value);
        await OperateEdge(surgeon, torso, "SurgeryGraphTorso", "OpenIncision");
        await OperateEdge(surgeon, torso, "SurgeryGraphTorso", "AttachLeftArm", arm);
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
        await Server.WaitAssertion(() => torso = _body.GetOrgan(patient, Torso)!.Value);
        await OperateEdge(surgeon, torso, "SurgeryGraphTorso", "OpenIncision");
        await OperateEdge(surgeon, torso, "SurgeryGraphTorso", "OpenBones");

        EntityUid selected = default;
        await Server.WaitAssertion(() =>
        {
            // The interaction layer passes the active-hand item, so make the implant active.
            selected = SSpawn("Pen");
            Assert.That(_hands.TryGetEmptyHand(surgeon, out var hand), Is.True);
            Assert.That(_hands.TryPickupAnyHand(surgeon, selected), Is.True);
            Assert.That(_hands.TrySetActiveHand(surgeon, hand), Is.True);
            Assert.That(_hands.GetActiveItem(surgeon), Is.EqualTo(selected));
            Assert.That(_graph.TryPerformEdgeStep(torso, surgeon, selected, "CavityImplant"), Is.True);
        });
        await RunSeconds(14);
        await Server.WaitAssertion(() =>
        {
            var containers = Server.System<SharedContainerSystem>();
            Assert.That(containers.TryGetContainer(torso, "_part_cavity", out var cavity), Is.True);
            Assert.That(cavity!.ContainedEntities, Does.Contain(selected));
            // Step 1 of the same edge retrieves the item into the now-free hand.
            Assert.That(_graph.TryPerformEdgeStep(torso, surgeon, _hands.GetActiveItemOrSelf(surgeon), "CavityImplant"), Is.True);
        });
        await RunSeconds(14);
        await Server.WaitAssertion(() =>
        {
            Assert.That(_hands.IsHolding(surgeon, selected), Is.True);
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
