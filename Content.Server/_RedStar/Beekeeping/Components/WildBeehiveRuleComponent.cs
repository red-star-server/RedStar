using Robust.Shared.Prototypes;

namespace Content.Server._RedStar.Beekeeping.Components;

[RegisterComponent]
public sealed partial class WildBeehiveRuleComponent : Component
{
    public bool SpawnedHive;

    /// <summary>
    /// The hive settles only in storages made from one of these, so lockers and crates, not fridges or machines.
    /// </summary>
    [DataField(required: true)]
    public List<EntProtoId> StorageParents = new();
}
