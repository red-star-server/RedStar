using System.Diagnostics.CodeAnalysis;
using Content.Shared._Trauma.Surgery.Graph.Conditions;
using Content.Shared.Body;
using Content.Shared.DoAfter;
using Content.Shared.Popups;
using Content.Shared.Tools.Systems;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;

namespace Content.Shared._Trauma.Surgery.Graph;

public sealed partial class SurgeryGraphSystem : EntitySystem
{
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private BodySystem _body = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedToolSystem _tool = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<OperatedComponent, SurgeryGraphStepDoAfterEvent>(OnStepDoAfter);

        InitializeInteraction();
    }

    #region Queries

    public bool TryGetGraph(OperatedComponent comp, [NotNullWhen(true)] out SurgeryGraphPrototype? graph)
    {
        graph = null;
        return comp.GraphId is { } id && _proto.TryIndex(id, out graph);
    }

    public static SurgeryGraphNode? GetNode(SurgeryGraphPrototype graph, string name)
        => graph.Nodes.Find(n => n.Name == name);

    public IEnumerable<SurgeryGraphEdge> GetAllEdges(SurgeryGraphNode node)
    {
        foreach (var edge in node.Edges)
        {
            yield return edge;
        }

        foreach (var packageId in node.Packages)
        {
            if (!_proto.TryIndex(packageId, out var package))
                continue;

            foreach (var edge in package.Edges)
            {
                yield return edge;
            }
        }
    }

    public SurgeryGraphEdge? GetEdge(SurgeryGraphPrototype graph, string currentNode, string edgeId)
    {
        if (GetNode(graph, currentNode) is not { } node)
            return null;

        foreach (var edge in GetAllEdges(node))
        {
            if (edge.Id == edgeId)
                return edge;
        }

        return null;
    }

    public bool EdgeConditionsMet(EntityUid target, SurgeryGraphEdge edge)
    {
        foreach (var condition in edge.Conditions)
        {
            if (!condition.CheckWithInvert(target, EntityManager))
                return false;
        }

        return true;
    }

    public bool HasAvailableEdge(EntityUid target, SurgeryGraphNode node)
    {
        foreach (var edge in GetAllEdges(node))
        {
            if (EdgeConditionsMet(target, edge))
                return true;
        }

        return false;
    }

    public static bool IsBusy(OperatedComponent comp) => comp.IsOperating;

    #endregion

    #region Public API

    public bool TryAssignGraph(EntityUid target, ProtoId<SurgeryGraphPrototype> graphId)
    {
        if (!_proto.TryIndex(graphId, out var graph) || GetNode(graph, graph.StartNode) is not { } start)
            return false;

        var comp = EnsureComp<OperatedComponent>(target);
        if (IsBusy(comp))
            return false;

        comp.GraphId = graph.ID;
        comp.CurrentNode = graph.StartNode;
        comp.ActiveEdgeId = null;
        comp.CompletedSteps = 0;
        Dirty(target, comp);
        return true;
    }

    public bool TryPerformStep(EntityUid target, EntityUid surgeon, EntityUid heldItem, ProtoId<OrganCategoryPrototype>? focusOrgan = null)
    {
        if (!TryComp<OperatedComponent>(target, out var comp) || IsBusy(comp))
            return false;

        if (!TryGetGraph(comp, out var graph) || GetNode(graph, comp.CurrentNode) is not { } node)
            return false;

        var candidates = new List<(SurgeryGraphEdge Edge, int Index)>();
        foreach (var edge in GetAllEdges(node))
        {
            if (edge.Steps.Count == 0 || !EdgeConditionsMet(target, edge))
                continue;

            if (focusOrgan is { } focus && !EdgeConcernsOrgan(edge, focus))
                continue;

            var index = GetStepIndex(comp, edge);
            if (TryMatchTool(edge.Steps[index], heldItem))
                candidates.Add((edge, index));
        }

        if (candidates.Count == 0)
            return false;

        var active = candidates.FindIndex(c => c.Edge.Id == comp.ActiveEdgeId && c.Index > 0);
        if (active >= 0)
        {
            var chosen = candidates[active];
            StartStep(surgeon, target, comp, chosen.Edge, chosen.Index, heldItem);
            return true;
        }

        var specific = candidates.FindAll(c => c.Edge.Steps[c.Index].Organ != null || c.Edge.Steps[c.Index].Quality != null);
        if (specific.Count > 0)
            candidates = specific;

        if (candidates.Count > 1)
        {
            _popup.PopupEntity(Loc.GetString("surgery-graph-ambiguous"), target, surgeon, PopupType.MediumCaution);
            return false;
        }

        var match = candidates[0];
        StartStep(surgeon, target, comp, match.Edge, match.Index, heldItem);
        return true;
    }

    private static bool EdgeConcernsOrgan(SurgeryGraphEdge edge, ProtoId<OrganCategoryPrototype> organ)
    {
        foreach (var condition in edge.Conditions)
        {
            if (condition is OrganPresentCondition present && present.Organ == organ)
                return true;
        }

        return false;
    }

    #endregion

    #region Step execution

    private static int GetStepIndex(OperatedComponent comp, SurgeryGraphEdge edge)
        => comp.ActiveEdgeId == edge.Id && comp.CompletedSteps < edge.Steps.Count
            ? comp.CompletedSteps
            : 0;

    private bool TryMatchTool(SurgeryStepEntry step, EntityUid heldItem)
    {
        if (step.AnyItem)
            return true;

        if (step.Organ is { } organ)
            return _body.GetCategory(heldItem) == organ;

        if (step.Quality is { } quality)
            return _tool.HasQuality(heldItem, quality);

        return false;
    }

    private void StartStep(EntityUid surgeon, EntityUid target, OperatedComponent comp, SurgeryGraphEdge edge, int stepIndex, EntityUid heldItem)
    {
        var step = edge.Steps[stepIndex];

        comp.ActiveEdgeId = edge.Id;
        comp.CompletedSteps = stepIndex;
        comp.IsOperating = true;

        var ev = new SurgeryGraphStepDoAfterEvent(edge.Id, stepIndex);
        var args = new DoAfterArgs(EntityManager, surgeon, step.Duration, ev, target, target: target, used: heldItem)
        {
            BreakOnMove = true,
            NeedHand = true,
            BreakOnHandChange = true,
            CancelDuplicate = true,
            DuplicateCondition = DuplicateConditions.SameEvent,
        };

        if (!_doAfter.TryStartDoAfter(args, out var doAfter))
        {
            comp.IsOperating = false;
            return;
        }

        comp.DoAfter = doAfter;
        Dirty(target, comp);

        if (step.Sound is { } sound)
            _audio.PlayPredicted(sound, target, surgeon);
    }

    private void OnStepDoAfter(Entity<OperatedComponent> ent, ref SurgeryGraphStepDoAfterEvent args)
    {
        var comp = ent.Comp;
        comp.IsOperating = false;
        comp.DoAfter = null;

        if (args.Cancelled || args.Handled)
            return;

        if (comp.ActiveEdgeId != args.EdgeId || comp.CompletedSteps != args.StepIndex)
            return;

        if (!TryGetGraph(comp, out var graph) || GetEdge(graph, comp.CurrentNode, args.EdgeId) is not { } edge)
            return;

        if (args.StepIndex >= edge.Steps.Count || !EdgeConditionsMet(ent, edge))
            return;

        var step = edge.Steps[args.StepIndex];
        var surgeon = args.User;
        var used = args.Used;

        if (used is { } item && !TryMatchTool(step, item))
        {
            args.Handled = true;
            return;
        }

        comp.CompletedSteps = args.StepIndex + 1;

        foreach (var effect in step.Effects)
        {
            effect.Apply(ent, surgeon, used, EntityManager);
        }

        HandleSanitization(ent, _body.GetBody(ent) ?? ent, surgeon);

        if (comp.CompletedSteps >= edge.Steps.Count)
        {
            foreach (var effect in edge.Effects)
            {
                effect.Apply(ent, surgeon, used, EntityManager);
            }

            AdvanceNode(ent, comp, graph, edge.Target);
        }

        Dirty(ent, comp);
        args.Handled = true;
    }

    private void AdvanceNode(EntityUid target, OperatedComponent comp, SurgeryGraphPrototype graph, string targetNode)
    {
        comp.CurrentNode = targetNode;
        comp.ActiveEdgeId = null;
        comp.CompletedSteps = 0;

        if (GetNode(graph, targetNode) is { } node && node.Edges.Count == 0 && node.Packages.Count == 0)
        {
            comp.GraphId = null;
            comp.CurrentNode = string.Empty;
        }

        Dirty(target, comp);
    }

    #endregion
}
