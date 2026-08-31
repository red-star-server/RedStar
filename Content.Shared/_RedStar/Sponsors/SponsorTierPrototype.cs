using Content.Shared.Preferences.Loadouts;
using Robust.Shared.Prototypes;

namespace Content.Shared._RedStar.Sponsors;

[Prototype]
public sealed partial class SponsorTierPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = string.Empty;

    [DataField(required: true)]
    public string Name = string.Empty;

    /// <summary>
    /// Optional lower tier whose perks and loadouts are inherited.
    /// </summary>
    [DataField]
    public ProtoId<SponsorTierPrototype>? Parent;

    [DataField]
    public bool PriorityJoin;

    [DataField]
    public bool OocColor;

    [DataField]
    public bool GhostColor;

    /// <summary>
    /// Loadouts unlocked directly by this tier.
    /// Parent tier loadouts are inherited automatically.
    /// </summary>
    [DataField]
    public HashSet<ProtoId<LoadoutPrototype>> Loadouts = [];
}
