using Content.Server.Construction;
using Content.Server.Stack;
using Content.Shared._RedStar.Beekeeping.Components;
using Content.Shared._RedStar.Beekeeping.Events;
using Content.Shared._RedStar.Beekeeping.Visuals;
using Content.Shared.Chemistry.Components;
using Content.Shared.Destructible;
using Content.Shared.DoAfter;
using Content.Shared.Popups;
using Content.Shared.Stacks;
using Robust.Shared.Containers;

namespace Content.Server._RedStar.Beekeeping.Systems;

/// <summary>
/// The server side of cutting a frame (the tool check and the progress bar are shared), in two steps on a capped frame:
/// the first cut takes the wax caps off, the second the combs. A frame that is not capped yet gives its combs in one cut
/// and no wax. The frame stays and goes back into the hive empty.
/// </summary>
public sealed partial class BeehiveSystem
{
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private StackSystem _stack = default!;
    [Dependency] private SharedContainerSystem _container = default!;

    protected override Type[] FrameInteractBefore => new[] { typeof(ConstructionSystem) };

    [SubscribeLocalEvent]
    private void OnFrameCut(Entity<BeeFrameComponent> ent, ref BeeFrameCutDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        args.Handled = true;

        var combs = ent.Comp.Combs;
        if (combs <= 0)
            return;

        // First cut on a capped frame: only the wax caps come off, the honey stays for the second cut.
        if (GetFill(ent.Comp) == BeeFrameFill.Wax)
        {
            ent.Comp.Uncapped = true;
            Dirty(ent);
            UpdateFrameAppearance(ent);
            var waxEnt = SpawnNextToOrDrop(ent.Comp.WaxPrototype, ent);
            if (TryComp<StackComponent>(waxEnt, out var waxStack))
                _stack.SetCount((waxEnt, waxStack), ent.Comp.WaxPerCappedFrame);
            _popup.PopupEntity(Loc.GetString("beekeeping-frame-cut-caps", ("wax", ent.Comp.WaxPerCappedFrame)),
                ent, args.User);
            UpdateFrameHive(ent);
            return;
        }

        ent.Comp.Combs = 0;
        ent.Comp.Uncapped = false;
        Dirty(ent);
        UpdateFrameAppearance(ent);
        DropCombs(ent, combs, ent);

        _popup.PopupEntity(Loc.GetString("beekeeping-frame-cut-combs", ("combs", combs)), ent, args.User);

        UpdateFrameHive(ent);
    }

    /// <summary>
    /// One stack of combs next to <paramref name="near"/>. A stack's solution is one comb's worth, so each comb gets
    /// its share of the payload.
    /// </summary>
    private void DropCombs(Entity<BeeFrameComponent> frame, int combs, EntityUid near)
    {
        var payload = frame.Comp.Payload;
        frame.Comp.Payload = new Solution();
        var comb = SpawnNextToOrDrop(frame.Comp.CombPrototype, near);
        if (TryComp<StackComponent>(comb, out var combStack))
            _stack.SetCount((comb, combStack), combs);
        if (payload.Volume > 0 && Solutions.TryGetSolution(comb, frame.Comp.CombSolution, out var combSolution, out _))
            Solutions.TryAddSolution(combSolution.Value, payload.SplitSolution(payload.Volume / combs));
    }

    /// <summary>
    /// A frame broken or taken apart with a crowbar leaves its combs behind. Deleting it any other way (a map going
    /// away, an admin) gives nothing.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnFrameDestroyed(Entity<BeeFrameComponent> ent, ref DestructionEventArgs args)
    {
        var combs = ent.Comp.Combs;
        if (combs <= 0)
            return;

        ent.Comp.Combs = 0;
        DropCombs(ent, combs, ent);
    }
}
