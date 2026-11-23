namespace Content.Server._RedStar.Beekeeping.Components;

/// <summary>
/// A swarm released by the wild beehive event: the station is told where it settled.
/// </summary>
[RegisterComponent]
public sealed partial class WildBeehiveEventSwarmComponent : Component
{
    [ViewVariables]
    public EntityUid Rule;
}
