using System.Numerics;
using Content.Shared._DV.Carrying;
using Content.Shared._RedStar.Emoting.Components;
using Content.Shared._RedStar.Emoting.Events;
using Content.Shared._RedStar.Emoting.Prototypes;
using Content.Shared._RedStar.Posing;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Animations;
using Robust.Shared.Prototypes;

namespace Content.Client._RedStar.Emoting;

public sealed partial class EmoteAnimationSystem : EntitySystem
{
    [Dependency] private IEyeManager _eye = default!;
    [Dependency] private AnimationPlayerSystem _animationPlayer = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private SpriteSystem _spriteSystem = default!;

    private const string AnimationKey = "emote-animation";
    private const float ApproachDurationFraction = 0.2f;
    private const float ReturnStartFraction = 0.75f;

    private readonly Dictionary<EntityUid, SpriteVisualState> _savedStates = [];

    [SubscribeNetworkEvent]
    private void OnAnimation(EmoteAnimationEvent args)
    {
        TryPlayAnimation(GetEntity(args.Entity), args.Animation);
    }

    [SubscribeNetworkEvent]
    private void OnPairedAnimation(PairedEmoteAnimationEvent args)
    {
        if (!TryGetEntity(args.Initiator, out var initiator) || initiator is not { } fromEntity ||
            !TryGetEntity(args.Target, out var target) || target is not { } toEntity ||
            !CanAnimate(fromEntity) || !CanAnimate(toEntity) ||
            !ProtoMan.TryIndex(args.InitiatorAnimation, out var initiatorAnimation) ||
            !ProtoMan.TryIndex(args.TargetAnimation, out var targetAnimation) ||
            !initiatorAnimation.TryGetLength(out _) || !targetAnimation.TryGetLength(out _))
            return;

        var approach = Vector2.Zero;

        if (TryComp(fromEntity, out TransformComponent? initiatorTransform) &&
            TryComp(toEntity, out TransformComponent? targetTransform))
        {
            var from = _transform.GetMapCoordinates(fromEntity, initiatorTransform);
            var to = _transform.GetMapCoordinates(toEntity, targetTransform);
            var delta = to.Position - from.Position;
            var distance = delta.Length();
            if (from.MapId == to.MapId && float.IsFinite(distance) && distance > 0.001f)
                approach = delta / distance * Math.Clamp(args.ApproachOffset, 0f, distance * 0.45f);
        }

        PlayAnimation(fromEntity, initiatorAnimation, approach);
        PlayAnimation(toEntity, targetAnimation, -approach);
    }

    [SubscribeLocalEvent]
    private void OnMobStateChanged(Entity<EmoteAnimationComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState == MobState.Alive)
            return;

        StopAnimation(ent);
    }

    [SubscribeLocalEvent]
    private void OnShutdown(Entity<EmoteAnimationComponent> ent, ref ComponentShutdown args)
    {
        StopAnimation(ent);
    }

    [SubscribeLocalEvent]
    private void OnAnimationCompleted(Entity<EmoteAnimationComponent> ent, ref AnimationCompletedEvent args)
    {
        if (args.Key == AnimationKey)
            RestoreVisualState(ent);
    }

    [SubscribeLocalEvent]
    private void OnStartedCarrying(Entity<CarryingComponent> ent, ref ComponentStartup args)
    {
        StopAnimation(ent.Owner);
    }

    [SubscribeLocalEvent]
    private void OnBecameCarried(Entity<BeingCarriedComponent> ent, ref ComponentStartup args)
    {
        StopAnimation(ent.Owner);
    }

    private void TryPlayAnimation(
        EntityUid uid,
        ProtoId<EmoteAnimationPrototype> animation,
        Vector2 approach = default)
    {
        if (!CanAnimate(uid))
            return;

        if (!ProtoMan.TryIndex(animation, out var prototype))
            return;

        PlayAnimation(uid, prototype, approach);
    }

    private bool CanAnimate(EntityUid uid)
    {
        return !Deleted(uid) && HasComp<EmoteAnimationComponent>(uid) && HasComp<SpriteComponent>(uid) &&
               !HasComp<CarryingComponent>(uid) && !HasComp<BeingCarriedComponent>(uid) &&
               !(TryComp<PosingComponent>(uid, out var posing) && posing.Active) &&
               !(TryComp<MobStateComponent>(uid, out var mobState) && mobState.CurrentState != MobState.Alive);
    }

    private void PlayAnimation(EntityUid uid, EmoteAnimationPrototype prototype, Vector2 approach)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite))
            return;

        if (!prototype.TryGetLength(out var length))
            return;

        StopAnimation(uid);

        // Sprite offsets are in screen space for noRot sprites, otherwise in entity-local space.
        if (approach != Vector2.Zero)
        {
            approach = sprite.NoRotation
                ? _eye.CurrentEye.Rotation.RotateVec(approach)
                : (-_transform.GetWorldRotation(uid)).RotateVec(approach);
        }

        var baseOffset = sprite.Offset;
        var baseRotation = sprite.Rotation;

        _savedStates[uid] = new SpriteVisualState(
            prototype.Offset.Count > 0 || approach != Vector2.Zero ? baseOffset : null,
            prototype.Rotation.Count > 0 ? baseRotation : null,
            prototype.Direction.Count > 0
                ? new DirectionOverrideState(
                    sprite.EnableDirectionOverride,
                    sprite.DirectionOverride)
                : null);

        var animation = new Animation
        {
            Length = length
        };

        if (prototype.Offset.Count > 0 || approach != Vector2.Zero)
            animation.AnimationTracks.Add(CreateOffsetTrack(prototype, sprite, baseOffset, approach, length));

        if (prototype.Rotation.Count > 0)
            animation.AnimationTracks.Add(CreateRotationTrack(prototype, baseRotation));

        if (prototype.Direction.Count > 0)
            animation.AnimationTracks.Add(CreateDirectionTrack(prototype, sprite));

        foreach (var frame in prototype.Direction)
        {
            if (frame.Time != TimeSpan.Zero)
                break;

            sprite.EnableDirectionOverride = true;
            sprite.DirectionOverride = frame.Direction;
        }

        _animationPlayer.Play(uid, animation, AnimationKey);
    }

    private static AnimationTrackProperty CreateOffsetTrack(
        EmoteAnimationPrototype prototype,
        SpriteComponent sprite,
        Vector2 baseOffset,
        Vector2 approach,
        TimeSpan length)
    {
        AnimationTrackProperty track = approach == Vector2.Zero
            ? new AnimationTrackComponentProperty
            {
                ComponentType = typeof(SpriteComponent),
                Property = nameof(SpriteComponent.Offset)
            }
            : new PairedOffsetAnimationTrack(sprite, baseOffset, approach, (float)length.TotalSeconds);
        track.InterpolationMode = prototype.OffsetInterpolation;

        var previousTime = TimeSpan.Zero;

        foreach (var frame in prototype.Offset)
        {
            var duration = frame.Time - previousTime;

            track.KeyFrames.Add(
                new AnimationTrackProperty.KeyFrame(
                    baseOffset + frame.Offset,
                    (float)duration.TotalSeconds));

            previousTime = frame.Time;
        }

        if (prototype.Offset.Count == 0)
            track.KeyFrames.Add(new AnimationTrackProperty.KeyFrame(baseOffset, 0f));

        return track;
    }

    private static float ApproachAmount(float progress)
    {
        return Math.Clamp(
            Math.Min(progress / ApproachDurationFraction, (1f - progress) / (1f - ReturnStartFraction)),
            0f,
            1f);
    }

    private static AnimationTrackComponentProperty CreateRotationTrack(
        EmoteAnimationPrototype prototype,
        Angle baseRotation)
    {
        var track = new AnimationTrackComponentProperty
        {
            ComponentType = typeof(SpriteComponent),
            Property = nameof(SpriteComponent.Rotation),
            InterpolationMode = prototype.RotationInterpolation
        };

        var previousTime = TimeSpan.Zero;

        foreach (var frame in prototype.Rotation)
        {
            var duration = frame.Time - previousTime;

            track.KeyFrames.Add(
                new AnimationTrackProperty.KeyFrame(
                    baseRotation + frame.Rotation,
                    (float)duration.TotalSeconds));

            previousTime = frame.Time;
        }

        return track;
    }

    private static DirectionAnimationTrack CreateDirectionTrack(
        EmoteAnimationPrototype prototype,
        SpriteComponent sprite)
    {
        var track = new DirectionAnimationTrack(sprite)
        {
            InterpolationMode = AnimationInterpolationMode.Previous
        };

        var previousTime = TimeSpan.Zero;

        foreach (var frame in prototype.Direction)
        {
            track.KeyFrames.Add(
                new AnimationTrackProperty.KeyFrame(
                    frame.Direction,
                    (float)(frame.Time - previousTime).TotalSeconds));

            previousTime = frame.Time;
        }

        return track;
    }

    public void CancelAnimation(EntityUid uid)
    {
        StopAnimation(uid);
    }

    private void StopAnimation(EntityUid uid)
    {
        _animationPlayer.Stop(uid, AnimationKey);
        RestoreVisualState(uid);
    }

    private void RestoreVisualState(EntityUid uid)
    {
        if (!_savedStates.Remove(uid, out var state))
            return;

        if (!TryComp<SpriteComponent>(uid, out var sprite))
            return;

        if (state.Offset is { } offset)
            _spriteSystem.SetOffset((uid, sprite), offset);

        if (state.Rotation is { } rotation)
            _spriteSystem.SetRotation((uid, sprite), rotation);

        if (state.Direction is not { } direction)
            return;

        sprite.EnableDirectionOverride = direction.Enabled;
        sprite.DirectionOverride = direction.Direction;
    }

    private readonly record struct SpriteVisualState(Vector2? Offset, Angle? Rotation, DirectionOverrideState? Direction);

    private readonly record struct DirectionOverrideState(bool Enabled, Direction Direction);

    // Let Robust interpolate the original offset; add the paired approach independently each frame.
    private sealed class PairedOffsetAnimationTrack(
        SpriteComponent sprite,
        Vector2 baseOffset,
        Vector2 approach,
        float duration) : AnimationTrackProperty
    {
        private Vector2 _offset;
        private float _elapsed;

        public override (int KeyFrameIndex, float FramePlayingTime) InitPlayback()
        {
            _offset = baseOffset;
            _elapsed = 0f;
            return base.InitPlayback();
        }

        public override (int KeyFrameIndex, float FramePlayingTime) AdvancePlayback(
            object context, int prevKeyFrameIndex, float prevPlayingTime, float frameTime)
        {
            _elapsed += frameTime;
            var playback = base.AdvancePlayback(context, prevKeyFrameIndex, prevPlayingTime, frameTime);
            if (!sprite.Deleted)
            {
                ((IAnimationProperties)sprite).SetAnimatableProperty(
                    nameof(SpriteComponent.Offset), _offset + approach * ApproachAmount(_elapsed / duration));
            }

            return playback;
        }

        protected override void ApplyProperty(object context, object value)
        {
            _offset = (Vector2)value;
        }
    }

    private sealed class DirectionAnimationTrack(SpriteComponent sprite) : AnimationTrackProperty
    {
        protected override void ApplyProperty(object context, object value)
        {
            if (sprite.Deleted)
                return;

            sprite.EnableDirectionOverride = true;
            sprite.DirectionOverride = (Direction)value;
        }
    }
}
