using Content.Shared._RedStar.Xenobiology.Components;
using Content.Shared._RedStar.Xenobiology.Events;
using Content.Shared._RedStar.Xenobiology.UI;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Robust.Server.GameObjects;
using Robust.Shared.Audio.Systems;

namespace Content.Server._RedStar.Xenobiology.Systems;

public sealed partial class XenobiologyScannerSystem : EntitySystem
{
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private UserInterfaceSystem _ui = default!;
    [Dependency] private SharedAudioSystem _audio = default!;

    [SubscribeLocalEvent]
    private void OnScannableAfterInteractUsing(Entity<XenobiologyScannableComponent> entity, ref AfterInteractUsingEvent args)
        => TryStartScan(args);

    private void TryStartScan(AfterInteractUsingEvent args)
    {
        if (args.Handled || !args.CanReach || !HasComp<XenobiologyScannerComponent>(args.Used))
            return;

        var doAfter = new DoAfterArgs(EntityManager, args.User, TimeSpan.FromSeconds(1),
            new XenobiologyScannerDoAfterEvent(), args.Used, target: args.Target, used: args.Used)
        {
            NeedHand = true,
            BreakOnMove = true
        };

        args.Handled = _doAfter.TryStartDoAfter(doAfter);
    }

    [SubscribeLocalEvent]
    private void OnDoAfter(Entity<XenobiologyScannerComponent> scanner, ref XenobiologyScannerDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled || args.Target is not { } target)
            return;

        if (!SendScan(scanner.Owner, args.User, target))
            return;

        _audio.PlayPvs(scanner.Comp.ScanSound, scanner.Owner);
        args.Handled = true;
    }

    private bool SendScan(EntityUid uiOwner, EntityUid user, EntityUid target)
    {
        if (!HasComp<XenobiologyScannableComponent>(target))
            return false;

        var scanEvent = new XenobiologyScanEvent(target, MetaData(target).EntityName, MetaData(target).EntityPrototype?.ID);
        RaiseLocalEvent(target, scanEvent);
        var scan = scanEvent.Build();

        if (!_ui.HasUi(uiOwner, XenobiologyScannerUiKey.Key))
            return false;

        _ui.OpenUi(uiOwner, XenobiologyScannerUiKey.Key, user);
        _ui.ServerSendUiMessage(uiOwner, XenobiologyScannerUiKey.Key, new XenobiologyScannerScannedMessage(scan), user);
        return true;
    }
}
