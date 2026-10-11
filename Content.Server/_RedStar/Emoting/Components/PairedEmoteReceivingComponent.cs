using Content.Shared.Alert;
using Robust.Shared.Prototypes;

namespace Content.Server._RedStar.Emoting.Components;

/// <summary>
/// The incoming paired emote offer shown as an alert to its recipient.
/// </summary>
[RegisterComponent]
[Access(typeof(PairedEmoteSystem))]
public sealed partial class PairedEmoteReceivingComponent : Component
{
    public EntityUid Initiator;
    public ProtoId<AlertPrototype> Alert;
}
