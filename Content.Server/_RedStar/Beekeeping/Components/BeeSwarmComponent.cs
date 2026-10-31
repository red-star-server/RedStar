
namespace Content.Server._RedStar.Beekeeping.Components;

/// <summary>
/// A swarm leaving its hive with the old queen. It is an NPC on an errand (<see cref="BeeErrandComponent"/>) that flies
/// to a nest site it picked, and builds a wild hive there with its bees. The queen item rides inside it; if the swarm
/// is gone before it lands, the queen drops where it was.
/// </summary>
[RegisterComponent]
public sealed partial class BeeSwarmComponent : Component
{
    /// <summary>
    /// Container holding the old queen.
    /// </summary>
    [DataField]
    public string Container = "queen";

    /// <summary>
    /// Bees that left with the swarm.
    /// </summary>
    [ViewVariables]
    public int Bees;

    /// <summary>
    /// The wild hive to build.
    /// </summary>
    [ViewVariables]
    public string WildHive = "WildBeehive";

    /// <summary>
    /// The locker, closet or crate the swarm moves into, if it found one. Null: a free wall corner.
    /// </summary>
    [ViewVariables]
    public EntityUid? Storage;

    /// <summary>
    /// A swarm of bees with no queen, leaving a crowded hive: it flies off and scatters instead of building a hive.
    /// </summary>
    [ViewVariables]
    public bool Scatter;

    /// <summary>
    /// A lone queen who saw no locker or crate yet: she flies short hops in random directions and looks again
    /// after each one.
    /// </summary>
    [ViewVariables]
    public bool Searching;

    /// <summary>
    /// How far she flies on one hop while searching.
    /// </summary>
    [DataField]
    public float SearchHop = 5f;

    /// <summary>
    /// The vent the swarm comes out of, if it left through the vents. Null once it is out.
    /// </summary>
    [ViewVariables]
    public EntityUid? VentExit;

    /// <summary>
    /// How far the swarm looks for a nest around where it is.
    /// </summary>
    [DataField]
    public float NestRange = 12f;

    /// <summary>
    /// The quirk rolled when the swarm left; the wild hive it builds keeps it.
    /// </summary>
    [ViewVariables]
    public string? Modifier;
}
