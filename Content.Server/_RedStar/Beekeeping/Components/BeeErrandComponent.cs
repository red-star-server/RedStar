using Content.Server._RedStar.Beekeeping.Data;
using Content.Server._RedStar.Beekeeping.Systems;
using Robust.Shared.Map;

namespace Content.Server._RedStar.Beekeeping.Components;

/// <summary>
/// A bee out of its hive on an errand: a forager showing a visit, the scout bringing home what it found, a meat
/// colony's bee after a creature, a swarm or a lone queen on the way to a new home. It is an NPC: its HTN task
/// (<c>BeeErrandCompound</c>) asks <see cref="BeeErrandSystem"/> where to fly next, flies there with the engine's
/// pathfinding and steering, and then does what the errand wants at that stop. The colony's numbers do not wait for it:
/// a visit's nectar is counted by the hive when the visit is made; the bee only acts it out.
/// </summary>
[RegisterComponent, Access(typeof(BeeErrandSystem))]
public sealed partial class BeeErrandComponent : Component
{
    [DataField]
    public BeeErrandKind Kind;

    /// <summary>
    /// How long the bee stays inside the flower.
    /// </summary>
    [DataField]
    public TimeSpan VisitTime = TimeSpan.FromSeconds(1.5);

    /// <summary>
    /// How close to a stop the bee has to fly before it acts there, in tiles.
    /// </summary>
    [DataField]
    public float StopRange = 1f;

    /// <summary>
    /// The hive the bee belongs to and goes back to.
    /// </summary>
    [ViewVariables]
    public EntityUid Hive;

    /// <summary>
    /// What it flies to: a tray, a creature, a locker to nest in.
    /// </summary>
    [ViewVariables]
    public EntityUid? Target;

    /// <summary>
    /// A spot to fly to when there is no target entity: a searching swarm's next hop.
    /// </summary>
    [ViewVariables]
    public EntityCoordinates? Spot;

    [ViewVariables]
    public BeeErrandStage Stage;

    /// <summary>
    /// A way through the pipes: the vent it dives into near the hive and the one it comes out of near the target. On
    /// the way back it goes the other way round.
    /// </summary>
    [ViewVariables]
    public EntityUid? VentEntry;

    [ViewVariables]
    public EntityUid? VentExit;

    /// <summary>
    /// The pipes are behind it on the way there, and on the way back.
    /// </summary>
    [ViewVariables]
    public bool PipedOut;

    [ViewVariables]
    public bool PipedBack;

    /// <summary>
    /// The scout's trays: the hive learns them when it lands.
    /// </summary>
    [ViewVariables]
    public List<EntityUid>? Found;

    /// <summary>
    /// What the bee is doing at its stop right now, which step of it, and when that step ends.
    /// </summary>
    [ViewVariables]
    public BeeErrandAct Act;

    [ViewVariables]
    public int ActStep;

    [ViewVariables]
    public TimeSpan ActStepEnd;

    /// <summary>
    /// While it dives into something (the flower, a vent, the hive) the bee glides from where it stopped to the middle
    /// of it: its world positions at both ends, and when the glide is over.
    /// </summary>
    [ViewVariables]
    public System.Numerics.Vector2? GlideFrom;

    [ViewVariables]
    public System.Numerics.Vector2 GlideTo;

    [ViewVariables]
    public TimeSpan GlideStart;

    [ViewVariables]
    public TimeSpan GlideEnd;

    /// <summary>
    /// Times the bee flew on to a target that had moved; past a few it gives up and goes home.
    /// </summary>
    [ViewVariables]
    public int Retargets;

    /// <summary>
    /// The bee is home or its errand ended some other way it is not lost by: nothing is taken from the colony when it
    /// goes.
    /// </summary>
    [ViewVariables]
    public bool Ended;
}
