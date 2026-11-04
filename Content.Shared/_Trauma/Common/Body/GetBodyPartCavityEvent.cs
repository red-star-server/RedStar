using Robust.Shared.Containers;

namespace Content.Shared._Trauma.Common.Body;

/// <summary>
/// Raised on a body part to find its cavity container, for storing an item via surgery.
/// </summary>
[ByRefEvent]
public record struct GetBodyPartCavityEvent(ContainerSlot? Container = null);
