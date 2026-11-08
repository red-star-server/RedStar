namespace Content.Server._RedStar.Beekeeping.Components;

/// <summary>
/// Marks a plant that a bee pollinated, until its next harvest. The one-off harvest bonus lives here;
/// <c>YieldMod</c> only carries it. While the marker is present the plant cannot be pollinated again.
/// </summary>
[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class BeePollinatedComponent : Component
{
    /// <summary>
    /// What the pollination added to <c>YieldMod</c>. Only this much is taken back when the marker goes, so whatever
    /// else changed the field in the meantime stays.
    /// </summary>
    [ViewVariables]
    public int AddedYieldMod;

    /// <summary>
    /// The pollination ends at this time even without a harvest.
    /// </summary>
    [ViewVariables, AutoPausedField]
    public TimeSpan ExpiresAt;

    /// <summary>
    /// Whether the roll succeeded and the next harvest is doubled.
    /// </summary>
    [ViewVariables]
    public bool Bonus;
}
