using Content.Server._RedStar.Research.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server._RedStar.Research.Components;

/// <summary>
/// A paid print job. Only active printers participate in the update query.
/// </summary>
[RegisterComponent, AutoGenerateComponentPause, Access(typeof(PointDiskConsoleSystem))]
public sealed partial class PointDiskPrintingComponent : Component
{
    [DataField]
    public EntProtoId Disk;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan FinishTime;

    /// <summary>
    /// Start of a power outage, used to postpone completion when power returns.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan? PowerLostAt;
}
