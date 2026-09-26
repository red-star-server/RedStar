using Content.Shared.Preferences.Loadouts;
using Robust.Shared.Prototypes;

namespace Content.Shared._RedStar.Sponsors;

[Prototype]
public sealed partial class SponsorTierPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = string.Empty;

    [DataField]
    public ProtoId<SponsorTierPrototype>? Parent;

    [DataField]
    public ulong? DiscordRoleId;

    [DataField]
    public bool PriorityJoin;

    [DataField]
    public Color? OocColor;

    [DataField]
    public bool GhostColor;

    [DataField]
    public HashSet<ProtoId<LoadoutPrototype>> Loadouts = [];
}
