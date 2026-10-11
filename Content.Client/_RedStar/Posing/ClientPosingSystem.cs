using System.Numerics;
using Content.Client._RedStar.Emoting;
using Content.Shared._RedStar.Posing;
using Content.Shared.Input;
using Robust.Client.GameObjects;
using Robust.Client.Input;
using Robust.Client.Player;

namespace Content.Client._RedStar.Posing;

public sealed partial class ClientPosingSystem : PosingSystem
{
    [Dependency] private IInputManager _input = default!;
    [Dependency] private IPlayerManager _player = default!;
    [Dependency] private SpriteSystem _sprite = default!;
    [Dependency] private EmoteAnimationSystem _emotes = default!;

    private readonly Dictionary<EntityUid, (Vector2 Offset, Angle Rotation)> _baseTransforms = [];

    public override void Initialize()
    {
        base.Initialize();

        var context = _input.Contexts.New("posing", "common");
        context.AddFunction(ContentKeyFunctions.TogglePosing);
        context.AddFunction(ContentKeyFunctions.PosingOffsetLeft);
        context.AddFunction(ContentKeyFunctions.PosingOffsetRight);
        context.AddFunction(ContentKeyFunctions.PosingOffsetUp);
        context.AddFunction(ContentKeyFunctions.PosingOffsetDown);
        context.AddFunction(ContentKeyFunctions.PosingRotateNegative);
        context.AddFunction(ContentKeyFunctions.PosingRotatePositive);
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _input.Contexts.Remove("posing");
    }

    [SubscribeLocalEvent]
    private void OnAfterHandleState(Entity<PosingComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        UpdatePose(ent);
    }

    [SubscribeLocalEvent]
    private void OnShutdown(Entity<PosingComponent> ent, ref ComponentShutdown args)
    {
        Restore(ent.Owner);
        if (_player.LocalEntity == ent.Owner)
            _input.Contexts.SetActiveContext("human");
    }

    protected override void OnPosingChanged(EntityUid uid, bool active)
    {
        UpdatePose((uid, Comp<PosingComponent>(uid)));
    }

    private void UpdatePose(Entity<PosingComponent> ent)
    {
        if (ent.Comp.Active)
        {
            if (!_baseTransforms.ContainsKey(ent.Owner) && TryComp<SpriteComponent>(ent, out var sprite))
            {
                _emotes.CancelAnimation(ent.Owner);
                _baseTransforms[ent.Owner] = (sprite.Offset, sprite.Rotation);
            }
        }
        else
        {
            Restore(ent.Owner);
        }

        if (_player.LocalEntity == ent.Owner)
            _input.Contexts.SetActiveContext(ent.Comp.Active ? "posing" : "human");
    }

    private void Restore(EntityUid uid)
    {
        if (!_baseTransforms.Remove(uid, out var original) || !TryComp<SpriteComponent>(uid, out var sprite))
            return;

        _sprite.SetOffset((uid, sprite), original.Offset);
        _sprite.SetRotation((uid, sprite), original.Rotation);
    }

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        var query = AllEntityQuery<PosingComponent, SpriteComponent>();
        while (query.MoveNext(out var uid, out var posing, out var sprite))
        {
            if (!posing.Active)
                continue;

            if (!_baseTransforms.TryGetValue(uid, out var original))
            {
                _emotes.CancelAnimation(uid);
                original = (sprite.Offset, sprite.Rotation);
                _baseTransforms[uid] = original;
            }

            _sprite.SetOffset((uid, sprite), original.Offset + posing.Offset);
            _sprite.SetRotation((uid, sprite), original.Rotation + posing.Rotation);
        }
    }
}
