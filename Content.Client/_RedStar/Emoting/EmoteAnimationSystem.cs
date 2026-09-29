using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using Content.Shared._RedStar.Emoting;
using Content.Shared._RedStar.Emoting.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Shared.Animations;

namespace Content.Client._RedStar.Emoting;

public sealed partial class EmoteAnimationSystem : EntitySystem
{
    [Dependency] private AnimationPlayerSystem _animationPlayer = default!;
    [Dependency] private SpriteSystem _sprite = default!;

    private const string AnimationKey = "emote-animation";

    private readonly Dictionary<EntityUid, (Vector2 Offset, Angle Rotation)> _savedTransforms = new();

    [SubscribeLocalEvent]
    private void OnHandleState(Entity<EmoteAnimationComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        if (ent.Comp.AnimationSequence == ent.Comp.LastClientAnimationSequence)
            return;

        ent.Comp.LastClientAnimationSequence = ent.Comp.AnimationSequence;

        if (ent.Comp.Animation == EmoteAnimationType.None)
            return;

        if (TryComp<MobStateComponent>(ent, out var mobState) &&
            mobState.CurrentState != MobState.Alive)
            return;

        PlayAnimation(ent, ent.Comp.Animation);
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
    private void OnAnimationCompleted(
        Entity<EmoteAnimationComponent> ent,
        ref AnimationCompletedEvent args)
    {
        if (args.Key != AnimationKey)
            return;

        RestoreTransform(ent);
    }

    private void PlayAnimation(EntityUid uid, EmoteAnimationType animation)
    {
        StopAnimation(uid);

        switch (animation)
        {
            case EmoteAnimationType.Flip:
                PlayFlip(uid);
                break;

            case EmoteAnimationType.Jump:
                PlayJump(uid);
                break;

            case EmoteAnimationType.Spin:
                PlaySpin(uid);
                break;

            case EmoteAnimationType.Dance:
                PlayDance(uid);
                break;

            case EmoteAnimationType.Tremble:
                PlayTremble(uid);
                break;
        }
    }

    private bool TryStartAnimation(
        EntityUid uid,
        [NotNullWhen(true)] out SpriteComponent? sprite)
    {
        if (!TryComp(uid, out sprite))
            return false;

        _savedTransforms[uid] = (sprite.Offset, sprite.Rotation);
        return true;
    }

    private void StopAnimation(EntityUid uid)
    {
        if (_animationPlayer.HasRunningAnimation(uid, AnimationKey))
            _animationPlayer.Stop(uid, AnimationKey);

        RestoreTransform(uid);
    }

    private void RestoreTransform(EntityUid uid)
    {
        if (!_savedTransforms.Remove(uid, out var saved))
            return;

        if (!TryComp<SpriteComponent>(uid, out var sprite))
            return;

        _sprite.SetOffset((uid, sprite), saved.Offset);
        _sprite.SetRotation((uid, sprite), saved.Rotation);
    }

    private void PlayFlip(EntityUid uid)
    {
        if (!TryStartAnimation(uid, out var sprite))
            return;

        var baseAngle = sprite.Rotation;

        var animation = new Animation
        {
            Length = TimeSpan.FromMilliseconds(500),
            AnimationTracks =
            {
                new AnimationTrackComponentProperty
                {
                    ComponentType = typeof(SpriteComponent),
                    Property = nameof(SpriteComponent.Rotation),
                    InterpolationMode = AnimationInterpolationMode.Linear,
                    KeyFrames =
                    {
                        new AnimationTrackProperty.KeyFrame(baseAngle, 0f),
                        new AnimationTrackProperty.KeyFrame(
                            Angle.FromDegrees(baseAngle.Degrees + 180),
                            0.25f),
                        new AnimationTrackProperty.KeyFrame(
                            Angle.FromDegrees(baseAngle.Degrees + 360),
                            0.5f)
                    }
                }
            }
        };

        _animationPlayer.Play(uid, animation, AnimationKey);
    }

    private void PlayJump(EntityUid uid)
    {
        if (!TryStartAnimation(uid, out var sprite))
            return;

        var baseOffset = sprite.Offset;

        var animation = new Animation
        {
            Length = TimeSpan.FromMilliseconds(500),
            AnimationTracks =
            {
                new AnimationTrackComponentProperty
                {
                    ComponentType = typeof(SpriteComponent),
                    Property = nameof(SpriteComponent.Offset),
                    InterpolationMode = AnimationInterpolationMode.Cubic,
                    KeyFrames =
                    {
                        new AnimationTrackProperty.KeyFrame(baseOffset, 0f),
                        new AnimationTrackProperty.KeyFrame(
                            baseOffset + new Vector2(0f, 0.3f),
                            0.125f),
                        new AnimationTrackProperty.KeyFrame(
                            baseOffset + new Vector2(0f, 0.7f),
                            0.25f),
                        new AnimationTrackProperty.KeyFrame(
                            baseOffset + new Vector2(0f, 0.3f),
                            0.375f),
                        new AnimationTrackProperty.KeyFrame(
                            baseOffset,
                            0.5f)
                    }
                }
            }
        };

        _animationPlayer.Play(uid, animation, AnimationKey);
    }

    private void PlaySpin(EntityUid uid)
    {
        if (!TryStartAnimation(uid, out var sprite))
            return;

        var baseAngle = sprite.Rotation;

        var animation = new Animation
        {
            Length = TimeSpan.FromMilliseconds(600),
            AnimationTracks =
            {
                new AnimationTrackComponentProperty
                {
                    ComponentType = typeof(SpriteComponent),
                    Property = nameof(SpriteComponent.Rotation),
                    InterpolationMode = AnimationInterpolationMode.Linear,
                    KeyFrames =
                    {
                        new AnimationTrackProperty.KeyFrame(baseAngle, 0f),
                        new AnimationTrackProperty.KeyFrame(
                            Angle.FromDegrees(baseAngle.Degrees + 180),
                            0.3f),
                        new AnimationTrackProperty.KeyFrame(
                            Angle.FromDegrees(baseAngle.Degrees + 360),
                            0.6f)
                    }
                }
            }
        };

        _animationPlayer.Play(uid, animation, AnimationKey);
    }

    private void PlayDance(EntityUid uid)
    {
        if (!TryStartAnimation(uid, out var sprite))
            return;

        var baseAngle = sprite.Rotation;

        var animation = new Animation
        {
            Length = TimeSpan.FromMilliseconds(900),
            AnimationTracks =
            {
                new AnimationTrackComponentProperty
                {
                    ComponentType = typeof(SpriteComponent),
                    Property = nameof(SpriteComponent.Rotation),
                    InterpolationMode = AnimationInterpolationMode.Linear,
                    KeyFrames =
                    {
                        new AnimationTrackProperty.KeyFrame(baseAngle, 0f),
                        new AnimationTrackProperty.KeyFrame(
                            Angle.FromDegrees(baseAngle.Degrees + 90),
                            0.075f),
                        new AnimationTrackProperty.KeyFrame(
                            Angle.FromDegrees(baseAngle.Degrees + 180),
                            0.15f),
                        new AnimationTrackProperty.KeyFrame(
                            Angle.FromDegrees(baseAngle.Degrees + 270),
                            0.225f),
                        new AnimationTrackProperty.KeyFrame(
                            Angle.FromDegrees(baseAngle.Degrees + 360),
                            0.3f),
                        new AnimationTrackProperty.KeyFrame(
                            Angle.FromDegrees(baseAngle.Degrees + 450),
                            0.375f),
                        new AnimationTrackProperty.KeyFrame(
                            Angle.FromDegrees(baseAngle.Degrees + 540),
                            0.45f),
                        new AnimationTrackProperty.KeyFrame(
                            Angle.FromDegrees(baseAngle.Degrees + 630),
                            0.525f),
                        new AnimationTrackProperty.KeyFrame(
                            Angle.FromDegrees(baseAngle.Degrees + 720),
                            0.6f),
                        new AnimationTrackProperty.KeyFrame(
                            baseAngle,
                            0.9f)
                    }
                }
            }
        };

        _animationPlayer.Play(uid, animation, AnimationKey);
    }

    private void PlayTremble(EntityUid uid)
    {
        if (!TryStartAnimation(uid, out var sprite))
            return;

        var baseOffset = sprite.Offset;

        var animation = new Animation
        {
            Length = TimeSpan.FromMilliseconds(400),
            AnimationTracks =
            {
                new AnimationTrackComponentProperty
                {
                    ComponentType = typeof(SpriteComponent),
                    Property = nameof(SpriteComponent.Offset),
                    InterpolationMode = AnimationInterpolationMode.Linear,
                    KeyFrames =
                    {
                        new AnimationTrackProperty.KeyFrame(baseOffset, 0f),
                        new AnimationTrackProperty.KeyFrame(
                            baseOffset + new Vector2(-0.06f, 0f),
                            0.05f),
                        new AnimationTrackProperty.KeyFrame(
                            baseOffset + new Vector2(0.06f, 0f),
                            0.10f),
                        new AnimationTrackProperty.KeyFrame(
                            baseOffset + new Vector2(-0.05f, 0f),
                            0.15f),
                        new AnimationTrackProperty.KeyFrame(
                            baseOffset + new Vector2(0.05f, 0f),
                            0.20f),
                        new AnimationTrackProperty.KeyFrame(
                            baseOffset + new Vector2(-0.03f, 0f),
                            0.25f),
                        new AnimationTrackProperty.KeyFrame(
                            baseOffset + new Vector2(0.03f, 0f),
                            0.30f),
                        new AnimationTrackProperty.KeyFrame(
                            baseOffset,
                            0.40f)
                    }
                }
            }
        };

        _animationPlayer.Play(uid, animation, AnimationKey);
    }
}
