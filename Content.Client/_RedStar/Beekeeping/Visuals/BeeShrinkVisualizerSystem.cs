using System.Numerics;
using Content.Shared._RedStar.Beekeeping.Components;
using Content.Shared._RedStar.Beekeeping.Visuals;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Shared.Animations;

namespace Content.Client._RedStar.Beekeeping.Visuals;

/// <summary>
/// Smoothly shrinks a flying bee into a flower or a hive and grows it back out, the same way an entity falls
/// into a chasm (<c>ChasmFallingVisualsSystem</c>). The server only says which size the bee is heading for.
/// </summary>
public sealed partial class BeeShrinkVisualizerSystem : VisualizerSystem<BeeShrinkVisualsComponent>
{
    private const string AnimationKey = "bee_shrink";

    /// <summary>
    /// Smallest scale the sprite is animated to. The engine refuses scales near zero (singular matrices), so
    /// the bee shrinks to this and is then hidden instead.
    /// </summary>
    private static readonly Vector2 SmallScale = new(0.05f, 0.05f);

    protected override void OnAppearanceChange(EntityUid uid,
        BeeShrinkVisualsComponent component,
        ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null ||
            !AppearanceSystem.TryGetData<BeeFlightSize>(uid, BeeForagerVisuals.Size, out var size, args.Component))
        {
            return;
        }

        if (component.AppliedSize == size)
            return;

        component.AppliedSize = size;
        var player = EnsureComp<AnimationPlayerComponent>(uid);
        if (AnimationSystem.HasRunningAnimation(player, AnimationKey))
            AnimationSystem.Stop((uid, player), AnimationKey);

        var sprite = (uid, args.Sprite);
        switch (size)
        {
            case BeeFlightSize.Full:
                SpriteSystem.SetVisible(sprite, true);
                SpriteSystem.SetScale(sprite, Vector2.One);
                break;
            case BeeFlightSize.Hidden:
                SpriteSystem.SetScale(sprite, SmallScale);
                SpriteSystem.SetVisible(sprite, false);
                break;
            case BeeFlightSize.Shrinking:
                SpriteSystem.SetVisible(sprite, true);
                AnimationSystem.Play((uid, player), ScaleAnimation(args.Sprite.Scale, SmallScale, component.ShrinkTime),
                    AnimationKey);
                break;
            case BeeFlightSize.Growing:
                SpriteSystem.SetScale(sprite, SmallScale);
                SpriteSystem.SetVisible(sprite, true);
                AnimationSystem.Play((uid, player), ScaleAnimation(SmallScale, Vector2.One, component.ShrinkTime),
                    AnimationKey);
                break;
        }
    }

    private static Animation ScaleAnimation(Vector2 from, Vector2 to, float seconds)
    {
        return new Animation
        {
            Length = TimeSpan.FromSeconds(seconds),
            AnimationTracks =
            {
                new AnimationTrackComponentProperty
                {
                    ComponentType = typeof(SpriteComponent),
                    Property = nameof(SpriteComponent.Scale),
                    KeyFrames =
                    {
                        new AnimationTrackProperty.KeyFrame(from, 0f),
                        new AnimationTrackProperty.KeyFrame(to, seconds),
                    },
                    InterpolationMode = AnimationInterpolationMode.Linear,
                },
            },
        };
    }
}
