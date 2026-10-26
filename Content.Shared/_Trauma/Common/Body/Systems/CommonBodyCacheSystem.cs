namespace Content.Shared._Trauma.Common.Body;

/// <summary>
/// Provides API for any module to look for cached organs in a body.
/// </summary>
public abstract class CommonBodyCacheSystem : EntitySystem
{
    /// <summary>
    /// Returns the organ cached for the requested category, or null if it is absent.
    /// </summary>
    public abstract EntityUid? GetOrgan(EntityUid body, [ForbidLiteral] string category);
}
