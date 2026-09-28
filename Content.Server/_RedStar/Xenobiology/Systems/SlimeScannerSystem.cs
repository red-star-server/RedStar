using Content.Shared._RedStar.Xenobiology.Components;
using Content.Shared._RedStar.Xenobiology.Events;
using Content.Shared._RedStar.Xenobiology.Slimes.Components;
using Content.Shared._RedStar.Xenobiology.UI;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;

namespace Content.Server._RedStar.Xenobiology.Systems;

public sealed partial class SlimeScannerSystem : EntitySystem
{
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private UserInterfaceSystem _ui = default!;
    [Dependency] private SlimeScanSystem _scan = default!;
    [Dependency] private SharedAudioSystem _audio = default!;

    private static readonly SoundPathSpecifier ScannerSound = new("/Audio/Items/Medical/healthscanner.ogg");

    [SubscribeLocalEvent]
    private void OnSlimeAfterInteractUsing(Entity<SlimeLifecycleComponent> entity, ref AfterInteractUsingEvent args)
        => TryStartScan(args);

    private void TryStartScan(AfterInteractUsingEvent args)
    {
        if (args.Handled || !args.CanReach || !HasComp<SlimeScannerComponent>(args.Used))
            return;

        var doAfter = new DoAfterArgs(EntityManager, args.User, TimeSpan.FromSeconds(1),
            new SlimeScannerDoAfterEvent(), args.Used, target: args.Target, used: args.Used)
        {
            NeedHand = true,
            BreakOnMove = true
        };

        args.Handled = _doAfter.TryStartDoAfter(doAfter);
    }

    [SubscribeLocalEvent]
    private void OnDoAfter(Entity<SlimeScannerComponent> scanner, ref SlimeScannerDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled || args.Target is not { } target)
            return;

        if (!SendScan(scanner.Owner, args.User, target))
            return;

        _audio.PlayPvs(ScannerSound, scanner.Owner);
        args.Handled = true;
    }

    private bool SendScan(EntityUid uiOwner, EntityUid user, EntityUid target)
    {
        if (_scan.TryBuildSlimeScanData(target) is not { } scan)
            return false;

        if (!_ui.HasUi(uiOwner, SlimeScannerUiKey.Key))
            return false;

        _ui.OpenUi(uiOwner, SlimeScannerUiKey.Key, user);
        _ui.ServerSendUiMessage(uiOwner, SlimeScannerUiKey.Key, new SlimeScannerScannedMessage(scan), user);
        return true;
    }
}
