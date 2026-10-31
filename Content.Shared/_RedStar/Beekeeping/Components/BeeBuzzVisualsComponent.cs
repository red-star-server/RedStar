using System.Numerics;

namespace Content.Shared._RedStar.Beekeeping.Components;

/// <summary>
/// A bee that never holds still on screen: the client wobbles its sprite around the real position, so an angry bee
/// circles over someone hiding in a locker instead of hanging in place, and is harder to click.
/// Purely visual; the bee itself is where its physics says.
/// </summary>
[RegisterComponent]
public sealed partial class BeeBuzzVisualsComponent : Component
{
    /// <summary>
    /// How far the sprite wanders from the real position, in tiles.
    /// </summary>
    [DataField]
    public float Amplitude = 0.3f;

    /// <summary>
    /// How fast it wanders, in rough circles per second.
    /// </summary>
    [DataField]
    public float Speed = 1.3f;

    /// <summary>
    /// For a swarm drawn as one sprite: how many layers make up one bee. Each such group wobbles on its own.
    /// Zero wobbles the whole sprite together.
    /// </summary>
    [DataField]
    public int LayerGroup;

    /// <summary>
    /// Wobbles only while the server says it flies (an item flying home); still in a hand or on the floor.
    /// </summary>
    [DataField]
    public bool WhileFlying;

    /// <summary>
    /// The layers' offsets from the prototype, read by the client before it starts moving them.
    /// </summary>
    [ViewVariables]
    public List<Vector2>? BaseOffsets;
}
