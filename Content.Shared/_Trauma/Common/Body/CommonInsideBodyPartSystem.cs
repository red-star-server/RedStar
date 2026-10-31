namespace Content.Shared._Trauma.Common.Body;

/// <summary>
/// Minimal API for BodySystem to use while keeping the actual code in goob module
/// </summary>
public abstract partial class CommonInsideBodyPartSystem : EntitySystem
{
    /// <summary>
    /// Updates the contained entity when it is inserted into a body-part cavity.
    /// </summary>
    public abstract void InsertedIntoPart(EntityUid item, EntityUid part);
    /// <summary>
    /// Clears cavity-related state when the contained entity is removed.
    /// </summary>
    public abstract void RemovedFromPart(EntityUid item);
}
