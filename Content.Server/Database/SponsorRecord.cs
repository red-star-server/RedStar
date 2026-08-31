namespace Content.Server.Database;

public sealed record SponsorRecord(
    Guid PlayerId,
    string Tier,
    string? OocColor,
    string? GhostColor);
