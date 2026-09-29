using System.Linq;
using System.Numerics;
using Content.Shared._RedStar.Emoting.Components;
using Content.Shared._RedStar.Emoting.Events;
using Content.Shared._RedStar.Emoting.Prototypes;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Client._RedStar.Emoting;

public sealed partial class EmoteAnimationSystem : EntitySystem
{
    [Dependency] private AnimationPlayerSystem _animationPlayer = default!;
    [Dependency] private IPrototypeManager _prototypeManager = default!;
    [Dependency] private SpriteSystem _spriteSystem = default!;
    [Dependency] private IGameTiming _timing = default!;

    private const string AnimationKey = "emote-animation";

    private readonly Dictionary<EntityUid, SpriteVisualState> _savedStates = [];
    private readonly Dictionary<EntityUid, ActiveEmoteAnimation> _activeAnimations = [];
    private readonly List<EntityUid> _finishedAnimations = [];

    [SubscribeNetworkEvent]
    private void OnAnimation(EmoteAnimationEvent args)
    {
        var uid = GetEntity(args.Entity);

        if (!HasComp<EmoteAnimationComponent>(uid))
            return;

        if (TryComp<MobStateComponent>(uid, out var mobState) &&
            mobState.CurrentState != MobState.Alive)
        {
            return;
        }

        if (!_prototypeManager.TryIndex(args.Animation, out var prototype))
            return;

        PlayAnimation(uid, prototype);
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

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_activeAnimations.Count == 0)
            return;

        _finishedAnimations.Clear();

        foreach (var (uid, active) in _activeAnimations)
        {
            if (!TryComp<SpriteComponent>(uid, out var sprite))
            {
                _finishedAnimations.Add(uid);
                continue;
            }

            var elapsed = _timing.CurTime - active.StartTime;

            ApplyDirectionFrames(sprite, active, elapsed);

            if (elapsed >= active.Length)
                _finishedAnimations.Add(uid);
        }

        foreach (var uid in _finishedAnimations)
        {
            FinishAnimation(uid);
        }
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
            baseOffset,
            baseRotation,
            sprite.EnableDirectionOverride,
            sprite.DirectionOverride);

        var animation = new Animation
        {
            Length = length
        };

        if (prototype.Offset.Count > 0)
            animation.AnimationTracks.Add(CreateOffsetTrack(prototype, baseOffset));

        if (prototype.Rotation.Count > 0)
            animation.AnimationTracks.Add(CreateRotationTrack(prototype, baseRotation));

        if (animation.AnimationTracks.Count > 0)
            _animationPlayer.Play(uid, animation, AnimationKey);

        var active = new ActiveEmoteAnimation(
            _timing.CurTime,
            length,
            prototype.Direction.OrderBy(frame => frame.Time).ToArray());

        _activeAnimations[uid] = active;

        ApplyDirectionFrames(sprite, active, TimeSpan.Zero);
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

        foreach (var frame in prototype.Offset.OrderBy(frame => frame.Time))
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

        foreach (var frame in prototype.Rotation.OrderBy(frame => frame.Time))
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

    private static void ApplyDirectionFrames(SpriteComponent sprite, ActiveEmoteAnimation active, TimeSpan elapsed)
    {
        while (active.NextDirectionFrame < active.DirectionFrames.Length)
        {
            var frame = active.DirectionFrames[active.NextDirectionFrame];

            if (frame.Time > elapsed)
                break;

            sprite.EnableDirectionOverride = true;
            sprite.DirectionOverride = frame.Direction;
            active.NextDirectionFrame++;
        }
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
        _activeAnimations.Remove(uid);

        if (_animationPlayer.HasRunningAnimation(uid, AnimationKey))
            _animationPlayer.Stop(uid, AnimationKey);

        RestoreVisualState(uid);
    }

    private void FinishAnimation(EntityUid uid)
    {
        _activeAnimations.Remove(uid);

        if (_animationPlayer.HasRunningAnimation(uid, AnimationKey))
            _animationPlayer.Stop(uid, AnimationKey);

        RestoreVisualState(uid);
    }

    private void RestoreVisualState(EntityUid uid)
    {
        if (!_savedStates.Remove(uid, out var state))
            return;

        if (!TryComp<SpriteComponent>(uid, out var sprite))
            return;

        _spriteSystem.SetOffset((uid, sprite), state.Offset);
        _spriteSystem.SetRotation((uid, sprite), state.Rotation);

        sprite.EnableDirectionOverride = state.EnableDirectionOverride;
        sprite.DirectionOverride = state.DirectionOverride;
    }

    private readonly record struct SpriteVisualState(
        Vector2 Offset,
        Angle Rotation,
        bool EnableDirectionOverride,
        Direction DirectionOverride);

    private sealed class ActiveEmoteAnimation(
        TimeSpan startTime,
        TimeSpan length,
        EmoteAnimationDirectionFrame[] directionFrames)
    {
        public readonly TimeSpan StartTime = startTime;
        public readonly TimeSpan Length = length;
        public readonly EmoteAnimationDirectionFrame[] DirectionFrames = directionFrames;

        public int NextDirectionFrame;
    }
}
