using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Content.Server.Database;

[Table("redstar_sponsors")]
public sealed class Sponsor
{
    [Key]
    public Guid PlayerId { get; set; }

    [MaxLength(64)]
    public string Tier { get; set; } = default!;

    [MaxLength(9)]
    public string? OocColor { get; set; }

    [MaxLength(9)]
    public string? GhostColor { get; set; }
}
