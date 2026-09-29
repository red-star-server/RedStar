using System.Numerics;
using Robust.Shared.Animations;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;

namespace Content.Shared._RedStar.Emoting.Prototypes;

/// <summary>
/// Defines a cosmetic sprite animation used by an emote.
/// All frame times are absolute times from the beginning of the animation.
/// Offsets and rotations are relative to the sprite's state when the animation starts.
/// </summary>
[Prototype]
public sealed partial class EmoteAnimationPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField]
    public AnimationInterpolationMode OffsetInterpolation = AnimationInterpolationMode.Linear;

    [DataField]
    public AnimationInterpolationMode RotationInterpolation = AnimationInterpolationMode.Linear;

    [DataField]
    public List<EmoteAnimationOffsetFrame> Offset = [];

    [DataField]
    public List<EmoteAnimationRotationFrame> Rotation = [];

    [DataField]
    public List<EmoteAnimationDirectionFrame> Direction = [];
}

[DataDefinition]
public sealed partial class EmoteAnimationOffsetFrame
{
    /// <summary>
    /// Absolute time from the beginning of the animation.
    /// </summary>
    [DataField(required: true)]
    public TimeSpan Time;

    /// <summary>
    /// Offset relative to the sprite's original offset.
    /// </summary>
    [DataField(required: true)]
    public Vector2 Offset;
}

[DataDefinition]
public sealed partial class EmoteAnimationRotationFrame
{
    /// <summary>
    /// Absolute time from the beginning of the animation.
    /// </summary>
    [DataField(required: true)]
    public TimeSpan Time;

    /// <summary>
    /// Rotation relative to the sprite's original rotation.
    /// </summary>
    [DataField(required: true)]
    public Angle Rotation;
}

[DataDefinition]
public sealed partial class EmoteAnimationDirectionFrame
{
    /// <summary>
    /// Absolute time from the beginning of the animation.
    /// </summary>
    [DataField(required: true)]
    public TimeSpan Time;

    /// <summary>
    /// Direction displayed by directional sprite layers.
    /// </summary>
    [DataField(required: true)]
    public Direction Direction;
}
