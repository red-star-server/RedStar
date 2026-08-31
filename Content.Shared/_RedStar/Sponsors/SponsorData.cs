using Robust.Shared.Serialization;

namespace Content.Shared._RedStar.Sponsors;

[Serializable, NetSerializable]
public sealed record SponsorData(
    string Tier,
    Color? OocColor,
    Color? GhostColor);

[Serializable, NetSerializable]
public sealed class SponsorDataChangedEvent(SponsorData? data) : EntityEventArgs
{
    public SponsorData? Data { get; } = data;
}
