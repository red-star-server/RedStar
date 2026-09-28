using Content.Server.Popups;
using Content.Server.Power.EntitySystems;
using Content.Server.Research.Systems;
using Content.Shared._RedStar.Xenobiology;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.DoAfter;
using Content.Shared.Verbs;
using Robust.Shared.Prototypes;

namespace Content.Server._RedStar.Xenobiology;

public sealed partial class XenobiologySampleAnalyzerSystem : EntitySystem
{
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private ItemSlotsSystem _slots = default!;
    [Dependency] private ResearchSystem _research = default!;
    [Dependency] private XenobiologyResearchSystem _xenobiology = default!;
    [Dependency] private PopupSystem _popup = default!;

    [SubscribeLocalEvent]
    private void OnGetVerbs(Entity<XenobiologySampleAnalyzerComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract || !this.IsPowered(ent, EntityManager) ||
            _slots.GetItemOrNull(ent.Owner, ent.Comp.SampleSlot) == null)
            return;

        var user = args.User;
        args.Verbs.Add(new AlternativeVerb
        {
            Text = Loc.GetString("xenobiology-analyzer-analyze-verb"),
            Act = () => StartAnalysis(ent, user)
        });
    }

    private void StartAnalysis(Entity<XenobiologySampleAnalyzerComponent> ent, EntityUid user)
    {
        if (!this.IsPowered(ent, EntityManager) ||
            _slots.GetItemOrNull(ent.Owner, ent.Comp.SampleSlot) is not { } sample)
            return;

        if (!_research.TryGetClientServer(ent.Owner, out var server, out _) ||
            !HasComp<XenobiologyResearchDatabaseComponent>(server.Value))
        {
            _popup.PopupEntity(Loc.GetString("xenobiology-analyzer-no-server"), ent, user);
            return;
        }

        if (!HasComp<SlimeExtractComponent>(sample) ||
            MetaData(sample).EntityPrototype is not { } prototype ||
            !_xenobiology.HasActiveSample(server.Value, new EntProtoId<SlimeExtractComponent>(prototype.ID)))
        {
            _popup.PopupEntity(Loc.GetString("xenobiology-analyzer-no-target"), ent, user);
            return;
        }

        var doAfter = new DoAfterArgs(EntityManager, user, TimeSpan.FromSeconds(ent.Comp.AnalysisTime),
            new XenobiologyAnalysisDoAfterEvent(), ent.Owner, target: sample)
        {
            BreakOnMove = true
        };
        _doAfter.TryStartDoAfter(doAfter);
    }

    [SubscribeLocalEvent]
    private void OnAnalysisComplete(Entity<XenobiologySampleAnalyzerComponent> ent,
        ref XenobiologyAnalysisDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled || args.Target is not { } sample ||
            !this.IsPowered(ent, EntityManager) ||
            _slots.GetItemOrNull(ent.Owner, ent.Comp.SampleSlot) != sample ||
            !HasComp<SlimeExtractComponent>(sample) ||
            !_research.TryGetClientServer(ent.Owner, out var server, out _))
            return;

        if (MetaData(sample).EntityPrototype is not { } prototype ||
            !_xenobiology.TryCompleteSample(server.Value, new EntProtoId<SlimeExtractComponent>(prototype.ID), out var reward))
            return;

        QueueDel(sample);
        _popup.PopupEntity(Loc.GetString("xenobiology-analyzer-complete", ("points", reward)), ent, args.User);
        args.Handled = true;
    }
}
