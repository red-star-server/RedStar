using Content.Shared._RedStar.Genetics;
using Content.Shared._RedStar.Xenobiology.Components.Container;
using Content.Shared._RedStar.Xenobiology.Components.Tools;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Popups;

namespace Content.Shared._RedStar.Xenobiology;

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

        string? failure = null;
        if (container.SampleCount >= container.MaxSamples)
            failure = "cell-collector-full";
        else if (!_genomes.IsGenerated(target))
            failure = "cell-collector-target-cant-collected";
        else if (TryComp<DamageableComponent>(target, out var damageable) &&
                 _damageable.GetPositiveDamage((target, damageable)).DamageDict.TryGetValue("Cellular", out var damage) &&
                 (float) damage >= ent.Comp.MaximumCellularDamage)
            failure = "cell-collector-sample-damaged";

        if (failure is not null)
        {
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
}
