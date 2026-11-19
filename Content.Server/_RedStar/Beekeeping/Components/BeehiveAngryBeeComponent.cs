using Content.Shared.Chemistry.Components;
using Content.Shared.FixedPoint;

namespace Content.Server._RedStar.Beekeeping.Components;

/// <summary>
/// An angry bee out of a hive. After <see cref="Lifetime"/> it calms down and flies back into the hive on a return errand
/// (<see cref="BeeErrandSystem.SendHome"/>); killed, or not home in time, it is lost to the colony. Its sting carries the payload it took from the queen.
/// Someone in a sealed suit (outer clothing that holds pressure plus a helmet) is not hurt by it.
/// </summary>
[RegisterComponent]
public sealed partial class BeehiveAngryBeeComponent : Component
{
    /// <summary>
    /// The hive the bee came out of.
    /// </summary>
    [ViewVariables]
    public EntityUid? Hive;

    /// <summary>
    /// This is the hive's scout.
    /// </summary>
    [DataField]
    public bool Scout;

    /// <summary>
    /// How long it stays angry before it flies home, and how long it may take to get there.
    /// </summary>
    [DataField]
    public TimeSpan Lifetime = TimeSpan.FromSeconds(60);

    [DataField]
    public TimeSpan ReturnTime = TimeSpan.FromSeconds(60);

    /// <summary>
    /// When it turns home, and when it is given up if it is not home by then.
    /// </summary>
    [ViewVariables]
    public TimeSpan ReturnAt;

    [ViewVariables]
    public TimeSpan? GiveUpAt;

    /// <summary>
    /// Whether the bee is still counted outside its hive. Cleared once, by whichever way it ends.
    /// </summary>
    [ViewVariables]
    public bool Counted;

    /// <summary>
    /// What the sting injects. Taken from the queen when the bee flies out.
    /// </summary>
    [ViewVariables]
    public Solution Payload = new();

    /// <summary>
    /// Payload one sting injects.
    /// </summary>
    [DataField]
    public FixedPoint2 PayloadPerSting = 1;

    /// <summary>
    /// Calmed by water or foam right now, with its AI asleep.
    /// </summary>
    [ViewVariables]
    public bool Calmed;
}
