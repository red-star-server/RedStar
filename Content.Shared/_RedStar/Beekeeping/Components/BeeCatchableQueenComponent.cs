namespace Content.Shared._RedStar.Beekeeping.Components;

/// <summary>
/// A queen flying off alone: a click takes her out of the flight and into the hand. Predicted, so she is in the hand
/// at once instead of after the server answers.
/// </summary>
[RegisterComponent]
public sealed partial class BeeCatchableQueenComponent : Component
{
    /// <summary>
    /// The container she rides in.
    /// </summary>
    [DataField]
    public string Container = "queen";
}
