namespace Content.Shared.Projectiles;

public sealed partial class ProjectileComponent
{
    /// <summary>
    /// Whether the projectile may only increase its target's damage.
    /// </summary>
    [DataField]
    public bool IncreaseOnly;
}
