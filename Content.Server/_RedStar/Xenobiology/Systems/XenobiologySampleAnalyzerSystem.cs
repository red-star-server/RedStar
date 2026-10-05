using Content.Server._RedStar.Xenobiology.Components;
using Content.Server.Power.EntitySystems;
using Content.Server.Research.Systems;
using Content.Shared._RedStar.Xenobiology.Components;
using Content.Shared._RedStar.Xenobiology.UI;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Power;
using Content.Shared.Research.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server._RedStar.Xenobiology.Systems;

public sealed partial class XenobiologySampleAnalyzerSystem : EntitySystem
{
    [Dependency] private ItemSlotsSystem _slots = default!;
    [Dependency] private ResearchSystem _research = default!;
    [Dependency] private XenobiologyResearchSystem _xenobiology = default!;
    [Dependency] private UserInterfaceSystem _ui = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private IGameTiming _timing = default!;

    [SubscribeLocalEvent]
    private void OnInserted(Entity<XenobiologySampleAnalyzerComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        ent.Comp.CompleteUntil = null;
        ent.Comp.LastReward = null;
        UpdateState(ent);
    }

    [SubscribeLocalEvent]
    private void OnRemoved(Entity<XenobiologySampleAnalyzerComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        if (ent.Comp.ProcessingSample == args.Entity)
            CancelAnalysis(ent.Comp);

        UpdateState(ent);
    }

    [SubscribeLocalEvent]
    private void OnRegistrationChanged(Entity<XenobiologySampleAnalyzerComponent> ent, ref ResearchRegistrationChangedEvent args)
    {
        UpdateState(ent);
    }

    [SubscribeLocalEvent]
    private void OnOpened(Entity<XenobiologySampleAnalyzerComponent> ent, ref BoundUIOpenedEvent args)
    {
        if (args.UiKey.Equals(XenobiologySampleAnalyzerUiKey.Key))
            UpdateState(ent);
    }

    [SubscribeLocalEvent]
    private void OnAnalyze(Entity<XenobiologySampleAnalyzerComponent> ent, ref XenobiologyAnalyzeSampleMessage args)
    {
        if (!args.UiKey.Equals(XenobiologySampleAnalyzerUiKey.Key) ||
            !_ui.IsUiOpen(ent.Owner, XenobiologySampleAnalyzerUiKey.Key, args.Actor) ||
            ent.Comp.ProcessingSample != null || !this.IsPowered(ent, EntityManager) ||
            !TryGetSample(ent, out var sample, out var prototype) ||
            !_research.TryGetClientServer(ent.Owner, out var server, out _) ||
            !HasComp<XenobiologyResearchDatabaseComponent>(server.Value) ||
            !_xenobiology.TryGetResearchPrototypeForSample(prototype, out _))
            return;

        ent.Comp.ProcessingSample = sample;
        ent.Comp.ProcessingServer = server.Value;
        ent.Comp.RemainingAnalysisTime = ent.Comp.AnalysisDuration;
        ent.Comp.AnalysisEndTime = _timing.CurTime + ent.Comp.AnalysisDuration;
        ent.Comp.CompleteUntil = null;
        ent.Comp.LastReward = null;
        UpdateState(ent);
    }

    [SubscribeLocalEvent]
    private void OnPowerChanged(Entity<XenobiologySampleAnalyzerComponent> ent, ref PowerChangedEvent args)
    {
        if (ent.Comp.ProcessingSample != null)
        {
            if (args.Powered && ent.Comp.AnalysisEndTime == null)
                ent.Comp.AnalysisEndTime = _timing.CurTime + ent.Comp.RemainingAnalysisTime;
            else if (!args.Powered && ent.Comp.AnalysisEndTime is { } endTime)
            {
                ent.Comp.RemainingAnalysisTime = TimeSpan.FromTicks(Math.Max(0, (endTime - _timing.CurTime).Ticks));
                ent.Comp.AnalysisEndTime = null;
            }
        }

        UpdateState(ent);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<XenobiologySampleAnalyzerComponent>();
        while (query.MoveNext(out var uid, out var analyzer))
        {
            if (analyzer.ProcessingSample is { } sample)
            {
                if (analyzer.ProcessingServer is not { } processingServer ||
                    !_research.TryGetClientServer(uid, out var server, out _) ||
                    server.Value != processingServer ||
                    !HasComp<XenobiologyResearchDatabaseComponent>(processingServer) ||
                    _slots.GetItemOrNull(uid, analyzer.SampleSlot) != sample ||
                    TerminatingOrDeleted(sample) || EntityManager.IsQueuedForDeletion(sample))
                {
                    CancelAnalysis(analyzer);
                    UpdateState((uid, analyzer));
                    continue;
                }

                if (analyzer.AnalysisEndTime is { } endTime && _timing.CurTime >= endTime)
                {
                    CompleteAnalysis((uid, analyzer), sample, processingServer);
                    continue;
                }

                if (_timing.CurTime < analyzer.NextUiUpdate)
                    continue;

                analyzer.NextUiUpdate = _timing.CurTime + TimeSpan.FromSeconds(0.2);
                UpdateState((uid, analyzer));
            }
            else if (analyzer.CompleteUntil is { } resetTime && _timing.CurTime >= resetTime)
            {
                analyzer.CompleteUntil = null;
                analyzer.LastReward = null;
                UpdateState((uid, analyzer));
            }
        }
    }

    private void CompleteAnalysis(Entity<XenobiologySampleAnalyzerComponent> ent, EntityUid sample, EntityUid server)
    {
        if (MetaData(sample).EntityPrototype is not { } prototype ||
            !_xenobiology.TryAnalyzeSample(server, new EntProtoId<XenobiologySampleComponent>(prototype.ID), out var reward))
        {
            CancelAnalysis(ent.Comp);
            UpdateState(ent);
            return;
        }

        CancelAnalysis(ent.Comp);
        ent.Comp.LastReward = reward;
        ent.Comp.CompleteUntil = _timing.CurTime + TimeSpan.FromSeconds(2);
        _audio.PlayPvs(ent.Comp.CompletionSound, ent.Owner);
        QueueDel(sample);
        UpdateState(ent);
    }

    private static void CancelAnalysis(XenobiologySampleAnalyzerComponent analyzer)
    {
        analyzer.ProcessingSample = null;
        analyzer.ProcessingServer = null;
        analyzer.AnalysisEndTime = null;
        analyzer.RemainingAnalysisTime = TimeSpan.Zero;
    }

    private bool TryGetSample(Entity<XenobiologySampleAnalyzerComponent> ent,
        out EntityUid sample,
        out EntProtoId<XenobiologySampleComponent> prototype)
    {
        sample = default;
        prototype = default;
        if (_slots.GetItemOrNull(ent.Owner, ent.Comp.SampleSlot) is not { } uid ||
            TerminatingOrDeleted(uid) || EntityManager.IsQueuedForDeletion(uid) ||
            !HasComp<XenobiologySampleComponent>(uid) || MetaData(uid).EntityPrototype is not { } proto)
            return false;

        sample = uid;
        prototype = new EntProtoId<XenobiologySampleComponent>(proto.ID);
        return true;
    }

    private void UpdateState(Entity<XenobiologySampleAnalyzerComponent> ent)
    {
        if (!_ui.IsUiOpen(ent.Owner, XenobiologySampleAnalyzerUiKey.Key))
            return;

        var hasSample = TryGetSample(ent, out var sample, out var prototype);
        var hasServer = _research.TryGetClientServer(ent.Owner, out var server, out _) &&
                        HasComp<XenobiologyResearchDatabaseComponent>(server.Value);
        var processing = ent.Comp.ProcessingSample != null;
        var status = processing ? XenobiologySampleStatus.Processing
            : ent.Comp.CompleteUntil != null ? XenobiologySampleStatus.Complete
            : !this.IsPowered(ent, EntityManager) ? XenobiologySampleStatus.Unpowered
            : !hasServer ? XenobiologySampleStatus.NoServer
            : !hasSample ? XenobiologySampleStatus.Empty
            : _xenobiology.TryGetResearchPrototypeForSample(prototype, out _) ? XenobiologySampleStatus.Ready
            : XenobiologySampleStatus.Unmatched;
        var remaining = ent.Comp.AnalysisEndTime is { } endTime
            ? endTime - _timing.CurTime
            : ent.Comp.RemainingAnalysisTime;
        var progress = processing && ent.Comp.AnalysisDuration > TimeSpan.Zero
            ? 1f - (float) (remaining.TotalSeconds / ent.Comp.AnalysisDuration.TotalSeconds)
            : status == XenobiologySampleStatus.Complete ? 1f : 0f;
        var state = new XenobiologySampleAnalyzerUiState(
            hasSample ? GetNetEntity(sample) : null,
            status,
            Math.Clamp(progress, 0f, 1f),
            ent.Comp.LastReward);
        _ui.SetUiState(ent.Owner, XenobiologySampleAnalyzerUiKey.Key, state);
    }
}
