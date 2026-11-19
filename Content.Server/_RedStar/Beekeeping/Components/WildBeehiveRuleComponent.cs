namespace Content.Server._RedStar.Beekeeping.Components;

/// <summary>
/// The station event that releases a wild bee swarm from a vent.
/// </summary>
[RegisterComponent]
public sealed partial class WildBeehiveRuleComponent : Component
{
    /// <summary>
    /// The swarm is out: a second start of the event does nothing.
    /// </summary>
    [ViewVariables]
    public bool Released;
}
