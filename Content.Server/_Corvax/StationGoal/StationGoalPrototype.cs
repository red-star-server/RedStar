using Content.Shared._RedStar.Paperwork;
using Robust.Shared.Prototypes;

namespace Content.Server._Corvax.StationGoal;

[Prototype]
public sealed partial class StationGoalPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public LocId Text { get; private set; }

    [DataField(required: true)]
    public ProtoId<PaperworkPrototype> Paperwork { get; private set; }

    [DataField]
    public int? MinPlayers;

    [DataField]
    public int? MaxPlayers;

    /// <summary>
    /// Goal may require certain items to complete. These items will be delivered to the station's cargo trade post.
    /// </summary>
    [DataField]
    public List<EntProtoId> Spawns = new();
}
