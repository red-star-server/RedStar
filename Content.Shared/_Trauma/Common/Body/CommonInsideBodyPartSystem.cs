namespace Content.Shared._Trauma.Common.Body;

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
