using System.Numerics;
using Content.Client._RedStar.Emoting.Components;
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

    private const string AnimationKey = "emote";

    private readonly Dictionary<EntityUid, uint> _lastSequences = new();

    [SubscribeLocalEvent]
    private void OnHandleState(Entity<EmoteAnimationComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        if (_lastSequences.TryGetValue(ent, out var sequence) &&
            sequence == ent.Comp.AnimationSequence)
        {
            return;
        }

        _lastSequences[ent] = ent.Comp.AnimationSequence;

        if (ent.Comp.Animation == EmoteAnimationType.None)
            return;

        if (TryComp<MobStateComponent>(ent, out var mobState) &&
            mobState.CurrentState != MobState.Alive)
        {
            return;
        }

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
        _lastSequences.Remove(ent);
    }

    [SubscribeLocalEvent]
    private void OnAnimationCompleted(
        Entity<EmoteAnimationComponent> ent,
        ref AnimationCompletedEvent args)
    {
        if (args.Key != AnimationKey)
            return;

        RestoreSprite(ent);
    }

    private void PlayAnimation(EntityUid uid, EmoteAnimationType animation)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite))
            return;

        StopAnimation(uid);

        var active = EnsureComp<ActiveEmoteAnimationComponent>(uid);
        active.StartOffset = sprite.Offset;
        active.StartRotation = sprite.Rotation;

        var anim = animation switch
        {
            EmoteAnimationType.Flip => GetFlipAnimation(sprite),
            EmoteAnimationType.Jump => GetJumpAnimation(sprite),
            EmoteAnimationType.Spin => GetSpinAnimation(sprite),
            EmoteAnimationType.Tremble => GetTrembleAnimation(sprite),
            _ => null,
        };

        if (anim == null)
        {
            RemCompDeferred<ActiveEmoteAnimationComponent>(uid);
            return;
        }

        _animationPlayer.Play(uid, anim, AnimationKey);
    }

    private void StopAnimation(EntityUid uid)
    {
        if (_animationPlayer.HasRunningAnimation(uid, AnimationKey))
            _animationPlayer.Stop(uid, AnimationKey);

        RestoreSprite(uid);
    }

    private void RestoreSprite(EntityUid uid)
    {
        if (!TryComp<ActiveEmoteAnimationComponent>(uid, out var active))
            return;

        if (TryComp<SpriteComponent>(uid, out var sprite))
        {
            _sprite.SetOffset((uid, sprite), active.StartOffset);
            _sprite.SetRotation((uid, sprite), active.StartRotation);
        }

        RemCompDeferred<ActiveEmoteAnimationComponent>(uid);
    }

    private static Animation GetFlipAnimation(SpriteComponent sprite)
    {
        var rotation = sprite.Rotation;

        return new Animation
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
                        new AnimationTrackProperty.KeyFrame(rotation, 0f),
                        new AnimationTrackProperty.KeyFrame(
                            Angle.FromDegrees(rotation.Degrees + 180),
                            0.25f),
                        new AnimationTrackProperty.KeyFrame(
                            Angle.FromDegrees(rotation.Degrees + 360),
                            0.25f),
                    },
                },
            },
        };
    }

    private static Animation GetJumpAnimation(SpriteComponent sprite)
    {
        var offset = sprite.Offset;

        return new Animation
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
                        new AnimationTrackProperty.KeyFrame(offset, 0f),
                        new AnimationTrackProperty.KeyFrame(
                            offset + new Vector2(0f, 0.3f),
                            0.125f),
                        new AnimationTrackProperty.KeyFrame(
                            offset + new Vector2(0f, 0.7f),
                            0.125f),
                        new AnimationTrackProperty.KeyFrame(
                            offset + new Vector2(0f, 0.3f),
                            0.125f),
                        new AnimationTrackProperty.KeyFrame(offset, 0.125f),
                    },
                },
            },
        };
    }

    private static Animation GetSpinAnimation(SpriteComponent sprite)
    {
        var rotation = sprite.Rotation;

        return new Animation
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
                        new AnimationTrackProperty.KeyFrame(rotation, 0f),
                        new AnimationTrackProperty.KeyFrame(
                            Angle.FromDegrees(rotation.Degrees + 180),
                            0.3f),
                        new AnimationTrackProperty.KeyFrame(
                            Angle.FromDegrees(rotation.Degrees + 360),
                            0.3f),
                    },
                },
            },
        };
    }

    private static Animation GetTrembleAnimation(SpriteComponent sprite)
    {
        var offset = sprite.Offset;

        return new Animation
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
                        new AnimationTrackProperty.KeyFrame(offset, 0f),
                        new AnimationTrackProperty.KeyFrame(
                            offset + new Vector2(-0.06f, 0f),
                            0.05f),
                        new AnimationTrackProperty.KeyFrame(
                            offset + new Vector2(0.06f, 0f),
                            0.05f),
                        new AnimationTrackProperty.KeyFrame(
                            offset + new Vector2(-0.05f, 0f),
                            0.05f),
                        new AnimationTrackProperty.KeyFrame(
                            offset + new Vector2(0.05f, 0f),
                            0.05f),
                        new AnimationTrackProperty.KeyFrame(
                            offset + new Vector2(-0.03f, 0f),
                            0.05f),
                        new AnimationTrackProperty.KeyFrame(
                            offset + new Vector2(0.03f, 0f),
                            0.05f),
                        new AnimationTrackProperty.KeyFrame(offset, 0.10f),
                    },
                },
            },
        };
    }
}
