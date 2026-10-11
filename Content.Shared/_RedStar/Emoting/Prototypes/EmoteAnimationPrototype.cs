using System.Numerics;
using Robust.Shared.Animations;
using Robust.Shared.Prototypes;

namespace Content.Shared._RedStar.Emoting.Prototypes;

/// <summary>
/// Defines a cosmetic sprite animation used by an emote.
/// All frame times are non-negative absolute times from the beginning of the animation,
/// in non-decreasing order. Equal times define instantaneous transitions.
/// Offsets and rotations are relative to the sprite's state when the animation starts.
/// </summary>
[Prototype]
public sealed partial class EmoteAnimationPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField]
    public AnimationInterpolationMode OffsetInterpolation = AnimationInterpolationMode.Linear;

    /// <summary>
    /// Linear rotation interpolates angles along the shortest path, not by total revolutions.
    /// Cubic interpolation is not supported for angles.
    /// </summary>
    [DataField]
    public AnimationInterpolationMode RotationInterpolation = AnimationInterpolationMode.Linear;

    [DataField]
    public List<EmoteAnimationOffsetFrame> Offset = [];

    [DataField]
    public List<EmoteAnimationRotationFrame> Rotation = [];

    [DataField]
    public List<EmoteAnimationDirectionFrame> Direction = [];

    /// <summary>
    /// Rejects tracks that would give AnimationPlayer negative or out-of-order frame durations.
    /// Equal times are valid for instantaneous transitions.
    /// </summary>
    public bool TryGetLength(out TimeSpan length)
    {
        length = TimeSpan.Zero;

        return ValidTrack(Offset, static frame => frame.Time, ref length) &&
               ValidTrack(Rotation, static frame => frame.Time, ref length) &&
               ValidTrack(Direction, static frame => frame.Time, ref length) &&
               length > TimeSpan.Zero;
    }

    private static bool ValidTrack<T>(IReadOnlyList<T> frames, Func<T, TimeSpan> getTime, ref TimeSpan length)
    {
        var previous = TimeSpan.Zero;
        foreach (var frame in frames)
        {
            var time = getTime(frame);
            if (time < previous)
                return false;

            previous = time;
            if (time > length)
                length = time;
        }

        return true;
    }
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
