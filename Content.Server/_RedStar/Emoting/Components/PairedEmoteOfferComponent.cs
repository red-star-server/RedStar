using Content.Shared._RedStar.Emoting.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Server._RedStar.Emoting.Components;

/// <summary>
/// Runtime state for an entity currently offering a paired emote.
/// This component is server-authoritative and only exists while the offer is active.
/// </summary>
[RegisterComponent]
[Access(typeof(PairedEmoteSystem))]
public sealed partial class PairedEmoteOfferComponent : Component
{
    public EntityUid Target;

    public ProtoId<PairedEmotePrototype> Emote;

    public TimeSpan ExpiresAt;
}
