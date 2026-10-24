namespace Content.Server._RedStar.Beekeeping.Components;

/// <summary>
/// A queen defending her destroyed hive. She attacks those who were near the hive when it broke, and anyone who
/// hits her. The queen item rides inside her; once she has seen no target for a while she lands, and the item is
/// dropped where she is. Killed, she is lost with the item.
/// </summary>
[RegisterComponent]
public sealed partial class BeehiveAngryQueenComponent : Component
{
    /// <summary>
    /// Container holding the queen item.
    /// </summary>
    [DataField]
    public string Container = "queen";

    /// <summary>
    /// Living mobs this close to the hive when it broke become her targets.
    /// </summary>
    [DataField]
    public float TargetRange = 5f;

    /// <summary>
    /// She keeps a target while she sees it this close in a straight line.
    /// </summary>
    [DataField]
    public float SightRange = 7f;

    /// <summary>
    /// She lands after this long without seeing any target.
    /// </summary>
    [DataField]
    public TimeSpan CalmAfterLost = TimeSpan.FromSeconds(15);

    /// <summary>
    /// She lands after this long if she never had a target.
    /// </summary>
    [DataField]
    public TimeSpan CalmWithoutTargets = TimeSpan.FromSeconds(5);

    /// <summary>
    /// She lands after this long at most, even with a target in sight.
    /// </summary>
    [DataField]
    public TimeSpan MaxAngry = TimeSpan.FromMinutes(3);

    [ViewVariables]
    public TimeSpan AngrySince;

    [ViewVariables]
    public TimeSpan LastSeenTarget;

    [ViewVariables]
    public bool HadTargets;

    [ViewVariables]
    public TimeSpan NextCheck;

    /// <summary>
    /// Calmed by water or foam right now, with her AI asleep.
    /// </summary>
    [ViewVariables]
    public bool Calmed;
}
