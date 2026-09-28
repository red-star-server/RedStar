using System.Linq;
using Content.Server._RedStar.Xenobiology.Components;
using Content.Server.Power.EntitySystems;
using Content.Server.Research.Systems;
using Content.Shared._RedStar.Xenobiology.Components;
using Content.Shared._RedStar.Xenobiology.Events;
using Content.Shared._RedStar.Xenobiology.UI;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.DoAfter;
using Content.Shared.Power;
using Robust.Server.GameObjects;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server._RedStar.Xenobiology.Systems;

public sealed partial class XenobiologySampleAnalyzerSystem : EntitySystem
{
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private ItemSlotsSystem _slots = default!;
    [Dependency] private ResearchSystem _research = default!;
    [Dependency] private XenobiologyResearchSystem _xenobiology = default!;
    [Dependency] private UserInterfaceSystem _ui = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedAudioSystem _audio = default!;

    private TimeSpan _nextUpdate;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        if (_timing.CurTime < _nextUpdate)
            return;

        _nextUpdate = _timing.CurTime + TimeSpan.FromSeconds(0.25);
        var query = EntityQueryEnumerator<XenobiologySampleAnalyzerComponent>();
        while (query.MoveNext(out var uid, out var analyzer))
        {
            var ent = new Entity<XenobiologySampleAnalyzerComponent>(uid, analyzer);
            if (analyzer.AnalysisDoAfter != null &&
                (!_doAfter.IsRunning(analyzer.AnalysisDoAfter) || !IsAnalysisValid(ent)))
                CancelAnalysis(ent);

            if (_ui.IsUiOpen(uid, XenobiologySampleAnalyzerUiKey.Key))
                UpdateState(ent);
        }
    }

    [SubscribeLocalEvent]
    private void OnOpened(Entity<XenobiologySampleAnalyzerComponent> ent, ref BoundUIOpenedEvent args)
    {
        if (!args.UiKey.Equals(XenobiologySampleAnalyzerUiKey.Key))
            return;

        ent.Comp.LastState = null;
        UpdateState(ent);
    }

    [SubscribeLocalEvent]
    private void OnAnalyze(Entity<XenobiologySampleAnalyzerComponent> ent, ref XenobiologyAnalyzeSampleMessage args)
    {
        if (!args.UiKey.Equals(XenobiologySampleAnalyzerUiKey.Key) ||
            !_ui.IsUiOpen(ent.Owner, XenobiologySampleAnalyzerUiKey.Key, args.Actor))
            return;

        StartAnalysis(ent, args.Actor);
    }

    private void StartAnalysis(Entity<XenobiologySampleAnalyzerComponent> ent, EntityUid user)
    {
        if (ent.Comp.AnalysisDoAfter != null || !this.IsPowered(ent, EntityManager) ||
            _slots.GetItemOrNull(ent.Owner, ent.Comp.SampleSlot) is not { } sample ||
            TerminatingOrDeleted(sample) || EntityManager.IsQueuedForDeletion(sample) ||
            !HasComp<SlimeExtractComponent>(sample) || MetaData(sample).EntityPrototype is not { } prototype ||
            !_research.TryGetClientServer(ent.Owner, out var server, out _) ||
            !_xenobiology.HasActiveSample(server.Value, new EntProtoId<SlimeExtractComponent>(prototype.ID)))
            return;

        ent.Comp.AnalysisServer = server;
        ent.Comp.Result = null;
        ent.Comp.AnalysisSample = sample;
        ent.Comp.AnalysisStart = _timing.CurTime;
        ent.Comp.AnalysisEnd = _timing.CurTime + ent.Comp.AnalysisTime;
        var generation = ++ent.Comp.AnalysisGeneration;
        var doAfter = new DoAfterArgs(EntityManager, user, ent.Comp.AnalysisTime,
            new XenobiologyAnalysisDoAfterEvent { Generation = generation }, ent.Owner, target: sample)
        {
            BreakOnMove = true
        };
        if (!_doAfter.TryStartDoAfter(doAfter, out var id))
        {
            ClearAnalysis(ent.Comp);
            return;
        }

        // Instant do-afters may have already completed and cleared the analysis.
        if (ent.Comp.AnalysisSample != null)
            ent.Comp.AnalysisDoAfter = id;
        UpdateState(ent);
    }

    private bool IsAnalysisValid(Entity<XenobiologySampleAnalyzerComponent> ent)
    {
        return this.IsPowered(ent, EntityManager) &&
               ent.Comp.AnalysisSample is { } sample &&
               !TerminatingOrDeleted(sample) && !EntityManager.IsQueuedForDeletion(sample) &&
               _slots.GetItemOrNull(ent.Owner, ent.Comp.SampleSlot) == sample &&
               HasComp<SlimeExtractComponent>(sample) &&
               MetaData(sample).EntityPrototype is { } prototype &&
               _research.TryGetClientServer(ent.Owner, out var server, out _) &&
               server == ent.Comp.AnalysisServer &&
               _xenobiology.HasActiveSample(server.Value, new EntProtoId<SlimeExtractComponent>(prototype.ID));
    }

    [SubscribeLocalEvent]
    private void OnAnalysisComplete(Entity<XenobiologySampleAnalyzerComponent> ent,
        ref XenobiologyAnalysisDoAfterEvent args)
    {
        if (args.Handled || ent.Comp.AnalysisSample == null || args.Generation != ent.Comp.AnalysisGeneration)
            return;

        var valid = !args.Cancelled && IsAnalysisValid(ent);
        var server = ent.Comp.AnalysisServer;
        ClearAnalysis(ent.Comp);
        args.Handled = true;

        if (valid && server is { } researchServer && args.Target is { } sample &&
            MetaData(sample).EntityPrototype is { } prototype &&
            _xenobiology.TryCompleteSample(researchServer, new EntProtoId<SlimeExtractComponent>(prototype.ID), out var reward))
        {
            ent.Comp.Result = new XenobiologyAnalysisResult(prototype.ID, reward);
            _audio.PlayPvs(ent.Comp.CompletionSound, ent.Owner);
            QueueDel(sample);
        }

        UpdateState(ent);
    }

    [SubscribeLocalEvent]
    private void OnPowerChanged(Entity<XenobiologySampleAnalyzerComponent> ent, ref PowerChangedEvent args)
    {
        if (!args.Powered)
            CancelAnalysis(ent);
        UpdateState(ent);
    }

    [SubscribeLocalEvent]
    private void OnShutdown(Entity<XenobiologySampleAnalyzerComponent> ent, ref ComponentShutdown args)
    {
        CancelAnalysis(ent);
    }

    private void CancelAnalysis(Entity<XenobiologySampleAnalyzerComponent> ent)
    {
        var id = ent.Comp.AnalysisDoAfter;
        ClearAnalysis(ent.Comp);
        _doAfter.Cancel(id);
    }

    private static void ClearAnalysis(XenobiologySampleAnalyzerComponent analyzer)
    {
        analyzer.AnalysisDoAfter = null;
        analyzer.AnalysisServer = null;
        analyzer.AnalysisSample = null;
        analyzer.AnalysisStart = null;
        analyzer.AnalysisEnd = null;
    }

    private void UpdateState(Entity<XenobiologySampleAnalyzerComponent> ent)
    {
        if (!_ui.IsUiOpen(ent.Owner, XenobiologySampleAnalyzerUiKey.Key))
            return;

        var hasServer = _research.TryGetClientServer(ent.Owner, out var server, out _) &&
                        HasComp<XenobiologyResearchDatabaseComponent>(server.Value);
        var targets = hasServer && server is { } researchServer ? _xenobiology.GetActiveTargets(researchServer) : [];
        var sample = _slots.GetItemOrNull(ent.Owner, ent.Comp.SampleSlot);
        if (sample is { } uid && (TerminatingOrDeleted(uid) || EntityManager.IsQueuedForDeletion(uid)))
            sample = null;

        // Keep the receipt visible until a new sample is inserted.
        if (sample != null)
            ent.Comp.Result = null;

        EntProtoId<SlimeExtractComponent>? prototype = null;
        if (sample is { } sampleUid && HasComp<SlimeExtractComponent>(sampleUid) &&
            MetaData(sampleUid).EntityPrototype is { } samplePrototype)
            prototype = new EntProtoId<SlimeExtractComponent>(samplePrototype.ID);

        var status = !this.IsPowered(ent, EntityManager) ? XenobiologySampleStatus.Unpowered
            : !hasServer ? XenobiologySampleStatus.NoServer
            : sample == null ? XenobiologySampleStatus.Empty
            : ent.Comp.AnalysisDoAfter != null ? XenobiologySampleStatus.Analyzing
            : targets.Any(target => target.Sample == prototype) ? XenobiologySampleStatus.Ready
            : XenobiologySampleStatus.Unmatched;
        var state = new XenobiologySampleAnalyzerUiState(targets,
            server is { } serverUid ? MetaData(serverUid).EntityName : null,
            GetNetEntity(sample), prototype, status, ent.Comp.AnalysisStart, ent.Comp.AnalysisEnd, ent.Comp.Result);
        if (ent.Comp.LastState is { } previous && previous.ServerName == state.ServerName &&
            previous.Sample == state.Sample && previous.SamplePrototype == state.SamplePrototype &&
            previous.Status == state.Status && previous.AnalysisStart == state.AnalysisStart &&
            previous.AnalysisEnd == state.AnalysisEnd && previous.Result == state.Result &&
            previous.Targets.SequenceEqual(state.Targets))
            return;

        ent.Comp.LastState = state;
        _ui.SetUiState(ent.Owner, XenobiologySampleAnalyzerUiKey.Key, state);
    }
}
