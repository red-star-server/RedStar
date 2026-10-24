using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared._RedStar.Beekeeping.Components;
using Content.Shared._RedStar.Beekeeping.Events;
using Content.Shared._RedStar.Beekeeping.UI;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Timing;

namespace Content.Shared._RedStar.Beekeeping.Systems;

/// <summary>
/// The apiary analyzer's scan and window, the same way the plant analyzer does it. The server fills the window:
/// most of what a hive knows never leaves it.
/// </summary>
public abstract partial class SharedApiaryAnalyzerSystem : EntitySystem
{
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private SharedInteractionSystem _interaction = default!;
    [Dependency] protected SharedUserInterfaceSystem UI = default!;
    [Dependency] private IGameTiming _timing = default!;

    /// <summary>
    /// A hive, a queen, or a bee in hand or on the floor. Flying bees are not items and cannot be scanned.
    /// </summary>
    public bool CanScan(EntityUid target)
    {
        return HasComp<BeehiveComponent>(target) || HasComp<BeeQueenComponent>(target) || HasComp<BeeItemComponent>(target);
    }

    [SubscribeLocalEvent]
    private void OnAfterInteract(Entity<ApiaryAnalyzerComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Handled || args.Target is not { } target || !args.CanReach || !CanScan(target))
            return;

        args.Handled = _doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager,
            args.User,
            ent.Comp.ScanDelay,
            new ApiaryAnalyzerDoAfterEvent(),
            ent,
            target: target,
            used: ent)
        {
            BreakOnMove = true,
            NeedHand = true,
        });
    }

    [SubscribeLocalEvent]
    private void OnDoAfter(Entity<ApiaryAnalyzerComponent> ent, ref ApiaryAnalyzerDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled || args.Args.Target is not { } target || !CanScan(target))
            return;

        args.Handled = true;
        _audio.PlayPredicted(ent.Comp.ScanningEndSound, ent.Owner, args.Args.User);

        ent.Comp.Target = target;
        ent.Comp.User = args.Args.User;
        Dirty(ent);

        UI.OpenUi(ent.Owner, ApiaryAnalyzerUiKey.Key, args.Args.User);
        UpdateUi(ent);
    }

    [SubscribeLocalEvent]
    private void OnUiRangeCheck(Entity<ApiaryAnalyzerComponent> ent, ref BoundUserInterfaceCheckRangeEvent args)
    {
        if (args.Result == BoundUserInterfaceRangeResult.Fail || !args.UiKey.Equals(ApiaryAnalyzerUiKey.Key))
            return;

        if (ent.Comp.User is not { } user || args.Actor.Owner != user || ent.Comp.Target is not { } target ||
            TerminatingOrDeleted(target) || !_interaction.InRangeUnobstructed(user, target))
        {
            args.Result = BoundUserInterfaceRangeResult.Fail;
        }
    }

    [SubscribeLocalEvent]
    private void OnUiClosed(Entity<ApiaryAnalyzerComponent> ent, ref BoundUIClosedEvent args)
    {
        if (args.UiKey.Equals(ApiaryAnalyzerUiKey.Key) && args.Actor == ent.Comp.User)
            Stop(ent);
    }

    // Both events are also raised on the client while it applies a server state, which already carries the cleared
    // fields and the closed window: running Stop again there would mispredict.
    [SubscribeLocalEvent]
    private void OnDropped(Entity<ApiaryAnalyzerComponent> ent, ref DroppedEvent args)
    {
        if (!_timing.ApplyingState)
            Stop(ent);
    }

    [SubscribeLocalEvent]
    private void OnInserted(Entity<ApiaryAnalyzerComponent> ent, ref EntGotInsertedIntoContainerMessage args)
    {
        if (!_timing.ApplyingState)
            Stop(ent);
    }

    private void Stop(Entity<ApiaryAnalyzerComponent> ent)
    {
        if (ent.Comp.Target == null && ent.Comp.User == null)
            return;

        ent.Comp.Target = null;
        ent.Comp.User = null;
        Dirty(ent);
        UI.CloseUi(ent.Owner, ApiaryAnalyzerUiKey.Key);
    }

    /// <summary>
    /// Sends the window what the target looks like now. Only the server knows enough to do it.
    /// </summary>
    protected virtual void UpdateUi(Entity<ApiaryAnalyzerComponent> ent)
    {
    }
}
