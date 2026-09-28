using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server._RedStar.Xenobiology;

/// <summary>
/// The base component all slime processors possess.
/// </summary>
[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class SlimeProcessorComponent : Component
{
    /// <summary>
    /// The amount of time it takes to process slime corpses.
    /// </summary>
    [DataField(required: true)]
    public TimeSpan ProcessingTime;

    /// <summary>
    /// How long between each slime acquire.
    /// </summary>
    [DataField(required: true)]
    public TimeSpan SlimeAcquireCooldown;

    /// <summary>
    /// When the processor can next attempt to collect a corpse.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextSlimeAcquireTime;

    [DataField]
    public SoundSpecifier? ProcessingSound = new SoundPathSpecifier("/Audio/Machines/blender.ogg");

    [ViewVariables]
    public EntityUid? AudioStream;

    /// <summary>
    /// The name of the container the slime corpses are stored in.
    /// </summary>
    public const string SlimeContainerName = "slimes";

    /// <summary>
    /// Container for dead slimes inserted in the processor.
    /// </summary>
    [ViewVariables]
    public Container SlimeContainer = default!;
}

/// <summary>
/// The component for slime processors which are processing slimes.
/// </summary>
[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class ActiveSlimeProcessorComponent : Component
{
    /// <summary>
    /// The moment in time when processing will be done.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan ProcessingEndTime;

    /// <summary>
    /// When power was lost. Null while processing is running.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan? PowerLossTime;
}
