using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared._RedStar.Genetics.Components;
using Content.Shared._RedStar.Genetics.Systems;
using Content.Shared._RedStar.Xenobiology.Components.Container;
using Content.Shared._RedStar.Xenobiology.Components.Tools;
using Content.Shared._RedStar.Xenobiology.Events;

namespace Content.Shared._RedStar.Xenobiology.Systems;

public abstract partial class SampleCollectorSystem : EntitySystem
{
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private GenomeSystem _genomes = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private DamageableSystem _damageable = default!;

    /// <summary>
    /// Handles sample transfers that do not require a biopsy delay.
    /// </summary>
    protected virtual void HandleContainerInteraction(Entity<SampleCollectorComponent> ent, ref BeforeRangedInteractEvent args)
    {
    }

    [SubscribeLocalEvent]
    private void OnInteract(Entity<SampleCollectorComponent> ent, ref BeforeRangedInteractEvent args)
    {
        if (args.Handled || !args.CanReach || args.Target is not { } target ||
            target == ent.Owner ||
            !TryComp<SampleContainerComponent>(ent, out var container))
            return;

        if (!HasComp<GenomeComponent>(target))
        {
            HandleContainerInteraction(ent, ref args);
            return;
        }

        if (!CanCollectBiopsy((ent.Owner, ent.Comp, container), target, out _, out var failure))
        {
            if (failure is not null)
                _popup.PopupEntity(Loc.GetString(failure), ent, args.User, PopupType.SmallCaution);
            return;
        }

        var doAfter = new DoAfterArgs(EntityManager, args.User, ent.Comp.Delay,
            new SampleCollectorDoAfter(), ent, target: target, used: ent)
        {
            NeedHand = true,
            BreakOnDamage = true,
            BreakOnMove = true,
            MovementThreshold = ent.Comp.MovementThreshold,
            BlockDuplicate = true
        };
        args.Handled = _doAfter.TryStartDoAfter(doAfter);
    }

    /// <summary>
    /// Checks collector capacity, genome readiness, and the donor's cellular damage.
    /// Returns current damage for sample generation and a failure message key when available.
    /// Does not change either entity or display a popup.
    /// </summary>
    public bool CanCollectBiopsy(
        Entity<SampleCollectorComponent, SampleContainerComponent?> collector,
        EntityUid target,
        out float cellularDamage,
        out string? failure)
    {
        cellularDamage = 0f;
        failure = null;
        if (collector.Owner == target || !Resolve(collector, ref collector.Comp2, logMissing: false))
            return false;

        if (collector.Comp2.SampleCount >= collector.Comp2.MaxSamples)
            failure = "cell-collector-full";
        else if (!_genomes.IsGenerated(target))
            failure = "cell-collector-target-cant-collected";
        else
        {
            if (TryComp<DamageableComponent>(target, out var damageable) &&
                _damageable.GetPositiveDamage((target, damageable)).DamageDict.TryGetValue("Cellular", out var damage))
                cellularDamage = (float) damage;

            if (cellularDamage >= collector.Comp1.MaximumCellularDamage)
                failure = "cell-collector-sample-damaged";
        }

        return failure is null;
    }
}
