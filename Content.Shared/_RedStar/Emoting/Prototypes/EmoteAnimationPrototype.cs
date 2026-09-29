using System.Numerics;
using Robust.Shared.Animations;
using Robust.Shared.Prototypes;

namespace Content.Shared._RedStar.Emoting.Prototypes;

[Prototype]
public sealed partial class EmoteAnimationPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public TimeSpan Length;

    [DataField]
    public AnimationInterpolationMode OffsetInterpolation = AnimationInterpolationMode.Linear;

    [DataField]
    public AnimationInterpolationMode RotationInterpolation = AnimationInterpolationMode.Linear;

    [DataField]
    public List<EmoteAnimationOffsetFrame> Offset = [];

    [DataField]
    public List<EmoteAnimationRotationFrame> Rotation = [];
}

[DataDefinition]
public sealed partial class EmoteAnimationOffsetFrame
{
    [DataField(required: true)]
    public TimeSpan Time;

    /// <summary>
    /// Relative offset from the sprite's original offset.
    /// </summary>
    [DataField(required: true)]
    public Vector2 Offset;
}

[DataDefinition]
public sealed partial class EmoteAnimationRotationFrame
{
    [DataField(required: true)]
    public TimeSpan Time;

    /// <summary>
    /// Relative rotation from the sprite's original rotation.
    /// </summary>
    [DataField(required: true)]
    public Angle Rotation;
}
