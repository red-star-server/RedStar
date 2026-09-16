using Content.Shared._RedStar.Paperwork;
using Content.Shared.Cargo.Prototypes;
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
    /// Cargo products delivered to the station when this goal is issued.
    /// </summary>
    [DataField]
    public List<StationGoalCargoEntry> StartingEquipment = [];
}

[DataDefinition]
public sealed partial class StationGoalCargoEntry
{
    /// <summary>
    /// Cargo product to deliver.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<CargoProductPrototype> Product;

    /// <summary>
    /// Number of orders of this product to deliver.
    /// </summary>
    [DataField]
    public int Amount = 1;
}
