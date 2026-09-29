using System.Linq;
using System.Numerics;
using Content.Shared._RedStar.Emoting.Components;
using Content.Shared._RedStar.Emoting.Events;
using Content.Shared._RedStar.Emoting.Prototypes;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Shared.Animations;
using Robust.Shared.Prototypes;

namespace Content.Client._RedStar.Emoting;

public sealed partial class EmoteAnimationSystem : EntitySystem
{
    [Dependency] private AnimationPlayerSystem _animationPlayer = default!;
    [Dependency] private SpriteSystem _spriteSystem = default!;

    private const string AnimationKey = "emote-animation";

    private readonly Dictionary<EntityUid, SpriteVisualState> _savedStates = [];

    [SubscribeNetworkEvent]
    private void OnAnimation(EmoteAnimationEvent args)
    {
        TryPlayAnimation(GetEntity(args.Entity), args.Animation);
    }

    [SubscribeNetworkEvent]
    private void OnPairedAnimation(PairedEmoteAnimationEvent args)
    {
        TryPlayAnimation(
            GetEntity(args.Initiator),
            args.InitiatorAnimation);

        TryPlayAnimation(
            GetEntity(args.Target),
            args.TargetAnimation);
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

    private void TryPlayAnimation(
        EntityUid uid,
        ProtoId<EmoteAnimationPrototype> animation)
    {
        if (!HasComp<EmoteAnimationComponent>(uid))
            return;

        if (TryComp<MobStateComponent>(uid, out var mobState) &&
            mobState.CurrentState != MobState.Alive)
        {
            return;
        }

        if (!ProtoMan.TryIndex(animation, out var prototype))
            return;

        PlayAnimation(uid, prototype);
    }

    private void PlayAnimation(EntityUid uid, EmoteAnimationPrototype prototype)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite))
            return;

        var length = GetAnimationLength(prototype);

        if (length <= TimeSpan.Zero)
            return;

        StopAnimation(uid);

        var baseOffset = sprite.Offset;
        var baseRotation = sprite.Rotation;

        _savedStates[uid] = new SpriteVisualState(
            prototype.Offset.Count > 0 ? baseOffset : null,
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

        if (prototype.Offset.Count > 0)
            animation.AnimationTracks.Add(CreateOffsetTrack(prototype, baseOffset));

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

    private static AnimationTrackComponentProperty CreateOffsetTrack(
        EmoteAnimationPrototype prototype,
        Vector2 baseOffset)
    {
        var track = new AnimationTrackComponentProperty
        {
            ComponentType = typeof(SpriteComponent),
            Property = nameof(SpriteComponent.Offset),
            InterpolationMode = prototype.OffsetInterpolation
        };

        var previousTime = TimeSpan.Zero;

        foreach (var frame in prototype.Offset)
        {
            var duration = frame.Time - previousTime;

            track.KeyFrames.Add(
                new AnimationTrackProperty.KeyFrame(
                    baseOffset + frame.Offset,
                    (float) duration.TotalSeconds));

            previousTime = frame.Time;
        }

        return track;
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
                    (float) duration.TotalSeconds));

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
                    (float) (frame.Time - previousTime).TotalSeconds));

            previousTime = frame.Time;
        }

        return track;
    }

    private static TimeSpan GetAnimationLength(EmoteAnimationPrototype prototype)
    {
        var offsetLength = prototype.Offset.Count > 0
            ? prototype.Offset.Max(frame => frame.Time)
            : TimeSpan.Zero;

        var rotationLength = prototype.Rotation.Count > 0
            ? prototype.Rotation.Max(frame => frame.Time)
            : TimeSpan.Zero;

        var directionLength = prototype.Direction.Count > 0
            ? prototype.Direction.Max(frame => frame.Time)
            : TimeSpan.Zero;

        return new[] { offsetLength, rotationLength, directionLength }.Max();
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

    private sealed class DirectionAnimationTrack(SpriteComponent sprite) : AnimationTrackProperty
    {
        protected override void ApplyProperty(object context, object value)
        {
            if (sprite.Deleted)
                return;

            sprite.EnableDirectionOverride = true;
            sprite.DirectionOverride = (Direction) value;
        }
    }
}
