using Content.Shared._RedStar.Xenobiology.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Mobs;
using Content.Server._RedStar.Genetics.Systems;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Whitelist;
using Content.Shared._RedStar.Genetics.Components;
using Content.Shared._RedStar.Xenobiology.Components.Container;
using Content.Shared._RedStar.Xenobiology.Components.Tools;
using Content.Shared._RedStar.Xenobiology.Events;
using Content.Shared._RedStar.Xenobiology.Systems;
using Content.Shared._RedStar.Xenobiology.Visuals;

namespace Content.Server._RedStar.Xenobiology.Systems;

/// <summary>
/// Handles biopsy collection and transfer between sample containers.
/// </summary>
public sealed partial class ServerSampleCollectorSystem : SampleCollectorSystem
{
    [Dependency] private EntityWhitelistSystem _entityWhitelist = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private ServerGenomeSystem _genomes = default!;
    [Dependency] private SampleSystem _samples = default!;
    [Dependency] private SampleFactorySystem _factory = default!;

    [SubscribeLocalEvent]
    private void OnCollectorSamplesChanged(Entity<SampleCollectorComponent> ent, ref SampleContainerChangedEvent args)
    {
        UpdateCollectorAppearance(ent);
    }

    [SubscribeLocalEvent]
    private void OnCorpseStateChanged(Entity<SampledCorpseComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState != MobState.Dead)
            RemComp<SampledCorpseComponent>(ent);
    }

    [SubscribeLocalEvent]
    private void OnCollectorMapInit(Entity<SampleCollectorComponent> ent, ref MapInitEvent args)
    {
        UpdateCollectorAppearance(ent);
    }

    protected override void HandleContainerInteraction(Entity<SampleCollectorComponent> ent, ref BeforeRangedInteractEvent args)
    {
        if (args.Handled || !args.CanReach || args.Target is null)
            return;

        if (HasComp<GenomeComponent>(args.Target.Value))
            return;

        if (!TryComp<SampleContainerComponent>(ent, out var collector) ||
            !TryComp<SampleContainerComponent>(args.Target, out var target) || ent.Owner == args.Target.Value)
            return;

        args.Handled = collector.Empty && target is { Empty: false, AllowCollection: true }
            ? CollectSample((ent.Owner, collector), (args.Target.Value, target), args.User)
            : TransferSample((ent.Owner, collector), (args.Target.Value, target), args.User);
    }

    [SubscribeLocalEvent]
    private void OnCollectorCollectDoAfter(Entity<SampleCollectorComponent> ent, ref SampleCollectorDoAfter args)
    {
        if (args.Handled || args.Cancelled || args.Target is null)
            return;

        var target = args.Target.Value;
        if (!CanCollectBiopsy(ent, target, out var damage, out var failure))
        {
            if (failure is not null)
                Popup.PopupEntity(Loc.GetString(failure), ent, args.Args.User, PopupType.SmallCaution);
            return;
        }

        if (!_genomes.TryGetSnapshot(target, out var genome) ||
            !_samples.AddSample(ent.Owner, _factory.GenerateBiopsySample(genome, damage, ent.Comp)))
            return;

        if (ent.Comp.Damage is not null)
            Damageable.TryChangeDamage(target, ent.Comp.Damage, origin: args.Args.User);

        if (_mobState.IsDead(target))
            EnsureComp<SampledCorpseComponent>(target);

        Popup.PopupEntity(Loc.GetString("sample-collector-collected"), ent, args.Args.User);
        args.Handled = true;
    }

    private bool CollectSample(Entity<SampleContainerComponent> ent,
        Entity<SampleContainerComponent> target, EntityUid user)
    {
        if (ent.Comp.SampleCount >= ent.Comp.MaxSamples)
        {
            Popup.PopupEntity(Loc.GetString("sample-collector-full"), ent, user, PopupType.SmallCaution);
            return false;
        }

        if (!_samples.MoveFirstSample(target.AsNullable(), ent.AsNullable()))
            return false;

        Popup.PopupEntity(Loc.GetString("sample-collector-collected"), ent, user);
        return true;
    }

    private bool TransferSample(Entity<SampleContainerComponent> ent,
        Entity<SampleContainerComponent> target, EntityUid user)
    {
        if (!target.Comp.AllowTransfer || target.Comp.ToolsTransferWhitelist is null ||
            _entityWhitelist.IsWhitelistFail(target.Comp.ToolsTransferWhitelist, ent) ||
            target.Comp.SampleCount >= target.Comp.MaxSamples)
            return false;

        if (ent.Comp.Empty)
        {
            Popup.PopupEntity(Loc.GetString("sample-collector-empty"), ent, user, PopupType.SmallCaution);
            return false;
        }

        if (!_samples.MoveFirstSample(ent.AsNullable(), target.AsNullable()))
            return false;

        Popup.PopupEntity(Loc.GetString("sample-collector-transfer"), ent, user);
        return true;
    }

    private void UpdateCollectorAppearance(Entity<SampleCollectorComponent, SampleContainerComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp2, logMissing: false))
            return;

        Appearance.SetData(ent, SampleCollectorVisuals.State, ent.Comp2.Empty);
    }
}
