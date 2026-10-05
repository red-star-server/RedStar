using System.Linq;
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

namespace Content.Server._RedStar.Xenobiology.Systems;

public sealed partial class XenobiologySampleAnalyzerSystem : EntitySystem
{
    [Dependency] private ItemSlotsSystem _slots = default!;
    [Dependency] private ResearchSystem _research = default!;
    [Dependency] private XenobiologyResearchSystem _xenobiology = default!;
    [Dependency] private UserInterfaceSystem _ui = default!;
    [Dependency] private SharedAudioSystem _audio = default!;

    [SubscribeLocalEvent]
    private void OnInserted(Entity<XenobiologySampleAnalyzerComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        UpdateState(ent);
    }

    [SubscribeLocalEvent]
    private void OnRemoved(Entity<XenobiologySampleAnalyzerComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        UpdateState(ent);
    }

    [SubscribeLocalEvent]
    private void OnRegistrationChanged(Entity<XenobiologySampleAnalyzerComponent> ent, ref ResearchRegistrationChangedEvent args)
    {
        UpdateState(ent);
    }

    [SubscribeLocalEvent]
    private void OnPointsChanged(Entity<XenobiologySampleAnalyzerComponent> ent, ref ResearchServerPointsChangedEvent args)
    {
        UpdateState(ent);
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

        AnalyzeSample(ent);
    }

    private void AnalyzeSample(Entity<XenobiologySampleAnalyzerComponent> ent)
    {
        if (!this.IsPowered(ent, EntityManager) ||
            _slots.GetItemOrNull(ent.Owner, ent.Comp.SampleSlot) is not { } sample ||
            TerminatingOrDeleted(sample) || EntityManager.IsQueuedForDeletion(sample) ||
            !HasComp<XenobiologySampleComponent>(sample) || MetaData(sample).EntityPrototype is not { } prototype ||
            !_research.TryGetClientServer(ent.Owner, out var server, out _))
            return;

        if (!_xenobiology.TryCompleteSample(server.Value, new EntProtoId<XenobiologySampleComponent>(prototype.ID), out var reward))
            return;

        ent.Comp.Result = new XenobiologyAnalysisResult(prototype.ID, reward);
        _audio.PlayPvs(ent.Comp.CompletionSound, ent.Owner);
        QueueDel(sample);
        UpdateState(ent);
    }

    [SubscribeLocalEvent]
    private void OnPowerChanged(Entity<XenobiologySampleAnalyzerComponent> ent, ref PowerChangedEvent args)
    {
        UpdateState(ent);
    }

    private void UpdateState(Entity<XenobiologySampleAnalyzerComponent> ent)
    {
        if (!_ui.IsUiOpen(ent.Owner, XenobiologySampleAnalyzerUiKey.Key))
            return;

        var hasServer = _research.TryGetClientServer(ent.Owner, out var server, out _) &&
                        HasComp<XenobiologyResearchDatabaseComponent>(server.Value);
        var targets = hasServer && server is { } researchServer ? _xenobiology.GetActiveTargets(researchServer) : [];
        var completedTargets = hasServer && server is { } completedServer ? _xenobiology.GetCompletedTargets(completedServer) : [];
        var remainingSamples = hasServer && server is { } remainingServer ? _xenobiology.GetRemainingSampleCount(remainingServer) : 0;
        var sample = _slots.GetItemOrNull(ent.Owner, ent.Comp.SampleSlot);
        if (sample is { } uid && (TerminatingOrDeleted(uid) || EntityManager.IsQueuedForDeletion(uid)))
            sample = null;

        // Keep the receipt visible until a new sample is inserted.
        if (sample != null)
            ent.Comp.Result = null;

        EntProtoId<XenobiologySampleComponent>? prototype = null;
        if (sample is { } sampleUid && HasComp<XenobiologySampleComponent>(sampleUid) &&
            MetaData(sampleUid).EntityPrototype is { } samplePrototype)
            prototype = new EntProtoId<XenobiologySampleComponent>(samplePrototype.ID);

        var status = !this.IsPowered(ent, EntityManager) ? XenobiologySampleStatus.Unpowered
            : !hasServer ? XenobiologySampleStatus.NoServer
            : sample == null ? XenobiologySampleStatus.Empty
            : targets.Any(target => target.Sample == prototype) ? XenobiologySampleStatus.Ready
            : XenobiologySampleStatus.Unmatched;
        var state = new XenobiologySampleAnalyzerUiState(targets, completedTargets, remainingSamples,
            server is { } serverUid ? MetaData(serverUid).EntityName : null,
            GetNetEntity(sample), prototype, status, ent.Comp.Result);
        if (ent.Comp.LastState is { } previous && previous.ServerName == state.ServerName &&
            previous.Sample == state.Sample && previous.SamplePrototype == state.SamplePrototype &&
            previous.Status == state.Status && previous.Result == state.Result &&
            previous.RemainingSamples == state.RemainingSamples &&
            previous.Targets.SequenceEqual(state.Targets) && previous.CompletedTargets.SequenceEqual(state.CompletedTargets))
            return;

        ent.Comp.LastState = state;
        _ui.SetUiState(ent.Owner, XenobiologySampleAnalyzerUiKey.Key, state);
    }
}
