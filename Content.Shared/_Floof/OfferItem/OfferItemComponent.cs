using Content.Shared.Alert;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Floof.OfferItem;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
[Access(typeof(SharedOfferItemSystem))]
public sealed partial class OfferItemComponent : Component
{
    /// <summary>
    /// Whether the user is selecting a recipient for the held item.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool IsInOfferMode;

    /// <summary>
    /// If this is true, then someone is currently offering an item to this entity, and <see cref="ReceivingFrom"/>
    /// stores the ID of that entity.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool IsInReceiveMode;

    [DataField, AutoNetworkedField]
    public string? Hand;

    [DataField, AutoNetworkedField]
    public EntityUid? Item;

    /// <summary>
    /// The recipient while offering, or the offerer while receiving.
    /// <see cref="IsInReceiveMode"/> identifies the receiving side.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? ReceivingFrom;

    [DataField]
    public float MaxOfferDistance = 2f;

    [DataField]
    public ProtoId<AlertPrototype> OfferAlert = "Offer";
}
