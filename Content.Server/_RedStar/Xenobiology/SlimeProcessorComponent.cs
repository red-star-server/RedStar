using Robust.Shared.Containers;

namespace Content.Server._RedStar.Xenobiology;

/// <summary>
/// The base component all slime processors possess.
/// </summary>
[RegisterComponent]
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
    [ViewVariables, AutoPausedField]
    public TimeSpan? ProcessingFinishedMoment;
}

/// <summary>
/// The component for slime processors which are collecting slimes.
/// </summary>
[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class CollectingSlimeProcessorComponent : Component
{
    /// <summary>
    /// The moment in time when another slime will be acquired.
    /// </summary>
    [ViewVariables, AutoPausedField]
    public TimeSpan? SlimeAcquireMoment;
}
