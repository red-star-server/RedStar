using Content.Shared._RedStar.Paperwork;
using Robust.Shared.Prototypes;

namespace Content.Server._Corvax.StationGoal;

[Prototype]
public sealed partial class StationGoalPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// The paperwork document sent to the station for this goal.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<PaperworkPrototype> Paperwork { get; private set; }

    /// <summary>
    /// The localized text used when publishing this goal as a news article.
    /// </summary>
    [DataField(required: true)]
    public LocId NewsText { get; private set; }

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
