using System.Numerics;
using Content.Shared._RedStar.Emoting.Components;
using Content.Shared._RedStar.Emoting.Prototypes;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.Client._RedStar.Emoting;

public sealed partial class EmoteAnimationSystem : EntitySystem
{
    [Dependency] private AnimationPlayerSystem _animationPlayer = default!;
    [Dependency] private IPrototypeManager _prototypeManager = default!;
    [Dependency] private SpriteSystem _spriteSystem = default!;

    private const string AnimationKey = "emote-animation";

    private readonly Dictionary<EntityUid, SpriteVisualState> _savedStates = new();

    [SubscribeLocalEvent]
    private void OnHandleState(Entity<EmoteAnimationComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        if (ent.Comp.AnimationSequence == ent.Comp.LastClientAnimationSequence)
            return;

        ent.Comp.LastClientAnimationSequence = ent.Comp.AnimationSequence;

        if (ent.Comp.Animation is not { } animationId)
            return;

        if (TryComp<MobStateComponent>(ent, out var mobState) &&
            mobState.CurrentState != MobState.Alive)
            return;

        if (!_prototypeManager.TryIndex(animationId, out var animation))
            return;

        PlayAnimation(ent, animation);
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
        if (args.Key != AnimationKey)
            return;

        RestoreVisualState(ent);
    }

    private void PlayAnimation(EntityUid uid, EmoteAnimationPrototype prototype)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite))
            return;

        StopAnimation(uid);

        var baseOffset = sprite.Offset;
        var baseRotation = sprite.Rotation;

        _savedStates[uid] = new SpriteVisualState(baseOffset, baseRotation);

        var animation = new Animation
        {
            Length = prototype.Length
        };

        if (prototype.Offset.Count > 0)
        {
            var track = new AnimationTrackComponentProperty
            {
                ComponentType = typeof(SpriteComponent),
                Property = nameof(SpriteComponent.Offset),
                InterpolationMode = prototype.OffsetInterpolation
            };

            foreach (var frame in prototype.Offset)
            {
                track.KeyFrames.Add(
                    new AnimationTrackProperty.KeyFrame(
                        baseOffset + frame.Offset,
                        (float)frame.Time.TotalSeconds));
            }

            animation.AnimationTracks.Add(track);
        }

        if (prototype.Rotation.Count > 0)
        {
            var track = new AnimationTrackComponentProperty
            {
                ComponentType = typeof(SpriteComponent),
                Property = nameof(SpriteComponent.Rotation),
                InterpolationMode = prototype.RotationInterpolation
            };

            foreach (var frame in prototype.Rotation)
            {
                track.KeyFrames.Add(
                    new AnimationTrackProperty.KeyFrame(
                        baseRotation + frame.Rotation,
                        (float)frame.Time.TotalSeconds));
            }

            animation.AnimationTracks.Add(track);
        }

        if (animation.AnimationTracks.Count == 0)
        {
            _savedStates.Remove(uid);
            return;
        }

        _animationPlayer.Play(uid, animation, AnimationKey);
    }

    private void StopAnimation(EntityUid uid)
    {
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
    }

    private readonly record struct SpriteVisualState(Vector2 Offset, Angle Rotation);
}
