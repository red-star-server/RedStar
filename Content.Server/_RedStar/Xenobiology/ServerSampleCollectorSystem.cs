using Content.Server._RedStar.Genetics;
using Content.Shared._RedStar.Genetics;
using Content.Server.Popups;
using Content.Shared.Damage.Systems;
using Content.Shared.Damage.Components;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Whitelist;
using Content.Shared._RedStar.Xenobiology;
using Content.Shared._RedStar.Xenobiology.Components.Container;
using Content.Shared._RedStar.Xenobiology.Components.Tools;
using Content.Shared._RedStar.Xenobiology.Events;
using Content.Shared._RedStar.Xenobiology.Visuals;
using Robust.Server.GameObjects;

namespace Content.Server._RedStar.Xenobiology;

/// <summary>
/// Handles biopsy collection and transfer between sample containers.
/// </summary>
public sealed partial class ServerSampleCollectorSystem : SampleCollectorSystem
{
    private enum SampleCollectorDirection
    {
        Collection,
        Transfer
    }

    [Dependency] private AppearanceSystem _appearance = default!;
    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private EntityWhitelistSystem _entityWhitelist = default!;
    [Dependency] private PopupSystem _popup = default!;
    [Dependency] private ServerGenomeSystem _genomes = default!;
    [Dependency] private SampleSystem _samples = default!;
    [Dependency] private SampleFactorySystem _factory = default!;

    [SubscribeLocalEvent]
    private void OnCollectorSamplesChanged(Entity<SampleCollectorComponent> ent, ref SampleContainerChangedEvent args)
    {
        UpdateCollectorAppearance(ent);
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

        TryComp<SampleContainerComponent>(args.Target, out var containerComponent);
        TryComp<SampleContainerComponent>(ent, out var collectorContainer);
        var direction = collectorContainer is { Empty: true } &&
            containerComponent is { Empty: false, AllowCollection: true }
            ? SampleCollectorDirection.Collection
            : SampleCollectorDirection.Transfer;
        args.Handled = CompleteCollectorInteraction(ent, args.Target.Value, direction, args.User);
    }

    [SubscribeLocalEvent]
    private void OnCollectorCollectDoAfter(Entity<SampleCollectorComponent> ent, ref SampleCollectorDoAfter args)
    {
        if (args.Handled || args.Cancelled || args.Target is null)
            return;

        args.Handled = CompleteCollectorInteraction(ent, args.Target.Value, SampleCollectorDirection.Collection, args.Args.User);
    }

    private bool CompleteCollectorInteraction(Entity<SampleCollectorComponent> ent, EntityUid target,
        SampleCollectorDirection direction, EntityUid user)
    {
        if (!CollectorInteractValidate(ent, target, direction, user))
            return false;

        switch (direction)
        {
            case SampleCollectorDirection.Collection:
                if (HasComp<GenomeComponent>(target))
                {
                    var damage = GetCellularDamage(target);
                    if (!_genomes.TryGetSnapshot(target, out var genome) || genome is null)
                        return false;

                    if (!_samples.AddSample(ent.Owner, _factory.GenerateBiopsySample(genome, damage, ent.Comp)))
                        return false;

                    if (ent.Comp.Damage is not null)
                        _damageable.TryChangeDamage(target, ent.Comp.Damage, origin: user);
                }
                else if (TryComp<SampleContainerComponent>(target, out var targetComp))
                {
                    if (!_samples.MoveFirstSample((target, targetComp), ent.Owner))
                        return false;
                }

                _popup.PopupEntity(Loc.GetString("cell-collector-collected"), ent, user);

                break;

            case SampleCollectorDirection.Transfer:
                if (!_samples.MoveFirstSample(ent.Owner, target))
                    return false;

                _popup.PopupEntity(Loc.GetString("cell-collector-transfer"), ent, user);
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(direction), direction, null);
        }

        return true;
    }

    private float GetCellularDamage(EntityUid source)
    {
        if (!TryComp<DamageableComponent>(source, out var damageable) ||
            !_damageable.GetPositiveDamage((source, damageable)).DamageDict.TryGetValue("Cellular", out var damage))
            return 0f;

        return (float) damage;
    }

    private void UpdateCollectorAppearance(Entity<SampleCollectorComponent, SampleContainerComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp2, logMissing: false))
            return;

        _appearance.SetData(ent, CellCollectorVisuals.State, ent.Comp2.Empty);
    }

    private bool CollectorInteractValidate(Entity<SampleCollectorComponent, SampleContainerComponent?> ent,
        Entity<SampleContainerComponent?> target,
        SampleCollectorDirection direction,
        EntityUid user)
    {
        if (ent.Owner == target.Owner)
            return false;

        if (!Resolve(ent, ref ent.Comp2))
            return false;

        TryComp(target.Owner, out target.Comp);

        switch (direction)
        {
            case SampleCollectorDirection.Collection:
                if (ent.Comp2.Samples.Count >= ent.Comp2.MaxSamples)
                {
                    _popup.PopupEntity(Loc.GetString("cell-collector-full"), ent, user, PopupType.SmallCaution);
                    return false;
                }

                var biological = HasComp<GenomeComponent>(target.Owner);
                switch (biological)
                {
                    case true when !_genomes.TryGetSnapshot(target.Owner, out _):
                        _popup.PopupEntity(Loc.GetString("cell-collector-target-cant-collected"), ent, user, PopupType.SmallCaution);
                        return false;
                    case true when GetCellularDamage(target.Owner) >= ent.Comp1.MaximumCellularDamage:
                        _popup.PopupEntity(Loc.GetString("cell-collector-sample-damaged"), ent, user, PopupType.SmallCaution);
                        return false;
                    case false when target.Comp is not { AllowCollection: true, Empty: false }:
                        _popup.PopupEntity(Loc.GetString("cell-collector-target-cant-collected"), ent, user, PopupType.SmallCaution);
                        return false;
                }

                break;

            case SampleCollectorDirection.Transfer:
                if (target.Comp == null ||
                    _entityWhitelist.IsWhitelistFail(target.Comp.ToolsTransferWhitelist, ent) ||
                    target.Comp.ToolsTransferWhitelist is null ||
                    !target.Comp.AllowTransfer ||
                    target.Comp.Samples.Count >= target.Comp.MaxSamples)
                    return false;

                if (ent.Comp2.Empty)
                {
                    _popup.PopupEntity(Loc.GetString("cell-collector-empty"), ent, user, PopupType.SmallCaution);
                    return false;
                }
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(direction), direction, null);
        }

        return true;
    }
}
