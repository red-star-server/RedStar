using Content.Shared._RedStar.Beekeeping.Data;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._RedStar.Beekeeping.UI;

[Serializable, NetSerializable]
public sealed class ApiaryTrayData
{
    public NetEntity Tray;
    public NetEntity? Plant;
    public EntProtoId? PlantPrototype;
    public ApiaryTrayState State;

    /// <summary>
    /// A creature of a meat hive instead of a tray. Nothing that identifies it is sent, no name and no entity: only its
    /// kind, the species for a humanoid (its localized name) or the prototype of an animal, and the plain picture of
    /// that kind.
    /// </summary>
    public bool Creature;
    public string? CreatureName;
    public EntProtoId? CreaturePrototype;
}
