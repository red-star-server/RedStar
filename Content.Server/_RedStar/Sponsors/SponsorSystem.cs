using System.Threading.Tasks;
using Content.Shared._RedStar.Sponsors;
using Content.Shared.Ghost.Components;
using Robust.Server.Player;
using Robust.Shared.Enums;
using Robust.Shared.Network;
using Robust.Shared.Player;

namespace Content.Server._RedStar.Sponsors;

public sealed partial class SponsorSystem : EntitySystem
{
    [Dependency] private IPlayerManager _players = default!;
    [Dependency] private ServerSponsorManager _manager = default!;

    public override void Initialize()
    {
        base.Initialize();

        _players.PlayerStatusChanged += OnPlayerStatusChanged;
    }

    public override void Shutdown()
    {
        _players.PlayerStatusChanged -= OnPlayerStatusChanged;
        base.Shutdown();
    }

    public async Task SyncPlayerAsync(NetUserId userId)
    {
        var data = await _manager.RefreshAsync(userId);

        if (_players.TryGetSessionById(userId, out var session))
        {
            RaiseNetworkEvent(new SponsorDataChangedEvent(data), session);

            if (session.AttachedEntity is { } entity && HasComp<GhostComponent>(entity))
                ApplyGhostColor(entity, userId);
        }
    }

    private async void OnPlayerStatusChanged(object? sender, SessionStatusEventArgs args)
    {
        if (args.NewStatus == SessionStatus.Connected)
        {
            await SyncPlayerAsync(args.Session.UserId);
            return;
        }

        if (args.NewStatus == SessionStatus.Disconnected)
            _manager.RemoveCached(args.Session.UserId);
    }

    [SubscribeLocalEvent]
    private void OnPlayerAttached(PlayerAttachedEvent args)
    {
        if (!HasComp<GhostComponent>(args.Entity))
            return;

        if (!TryComp<ActorComponent>(args.Entity, out var actor))
            return;

        ApplyGhostColor(args.Entity, actor.PlayerSession.UserId);
    }

    private void ApplyGhostColor(EntityUid entity, NetUserId userId)
    {
        if (!_manager.TryGetGhostColor(userId, out var color))
        {
            RemCompDeferred<SponsorGhostColorComponent>(entity);
            return;
        }

        var component = EnsureComp<SponsorGhostColorComponent>(entity);
        component.Color = color;
        Dirty(entity, component);
    }
}
