namespace Content.Shared._Trauma.Common.Body;

/// <summary>
/// Provides API for any module to look for cached organs in a body.
/// </summary>
public abstract class CommonBodyCacheSystem : EntitySystem
{
    public abstract EntityUid? GetOrgan(EntityUid body, [ForbidLiteral] string category);
}
