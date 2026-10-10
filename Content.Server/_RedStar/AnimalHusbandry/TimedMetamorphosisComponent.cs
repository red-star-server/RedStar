using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server._RedStar.AnimalHusbandry;

/// <summary>
/// Replaces an entity with another prototype after a fixed duration.
/// </summary>
[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class TimedMetamorphosisComponent : Component
{
    [DataField(required: true)]
    public EntProtoId Target;

    [DataField]
    public TimeSpan Duration = TimeSpan.FromMinutes(3);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoPausedField]
    public TimeSpan EndTime;
}
