using System.Numerics;
using Content.Server._RedStar.Beekeeping.Data;
using Content.Server._RedStar.Beekeeping.Systems;

namespace Content.Server._RedStar.Beekeeping.Components;

/// <summary>
/// A bee out of its hive on an errand: a forager showing a visit, the scout bringing home what it found, a bee taken
/// out of the hive and let go flying back. It is an NPC: its HTN task
/// (<c>BeeErrandCompound</c>) asks <see cref="BeeErrandSystem"/> where to fly next, flies there with the engine's
/// pathfinding and steering, and then does what the errand wants at that stop. The colony's numbers do not wait for it:
/// a visit's nectar is counted by the hive when the visit is made; the bee only acts it out.
/// </summary>
[RegisterComponent, AutoGenerateComponentPause, Access(typeof(BeeErrandSystem))]
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
    /// What it flies to: a tray.
    /// </summary>
    [ViewVariables]
    public EntityUid? Target;

    [ViewVariables]
    public BeeErrandStage Stage;

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

    [ViewVariables, AutoPausedField]
    public TimeSpan ActStepEnd;

    /// <summary>
    /// While it dives into something (the flower, the hive) the bee glides from where it stopped to the middle
    /// of it: its world positions at both ends, and when the glide is over.
    /// </summary>
    [ViewVariables]
    public Vector2? GlideFrom;

    [ViewVariables]
    public Vector2 GlideTo;

    [ViewVariables, AutoPausedField]
    public TimeSpan GlideStart;

    [ViewVariables, AutoPausedField]
    public TimeSpan GlideEnd;

    /// <summary>
    /// The bee is home or its errand ended some other way it is not lost by: nothing is taken from the colony when it
    /// goes.
    /// </summary>
    [ViewVariables]
    public bool Ended;
}
