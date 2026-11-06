using Content.Shared._RedStar.Beekeeping.Visuals;

namespace Content.Shared._RedStar.Beekeeping.Components;

/// <summary>
/// Lets a flying bee dive into a flower or a hive: on <see cref="BeeForagerVisuals.Size"/> the client scales the
/// sprite smoothly down to nothing and back up. The server waits <see cref="ShrinkTime"/> for each change.
/// </summary>
[RegisterComponent]
public sealed partial class BeeShrinkVisualsComponent : Component
{
    /// <summary>
    /// Seconds one shrink or growth takes.
    /// </summary>
    [DataField]
    public float ShrinkTime = 0.6f;

    /// <summary>
    /// Client only: the size last applied, so other appearance changes (the sprite turning) do not restart it.
    /// </summary>
    [ViewVariables]
    public BeeFlightSize? AppliedSize;
}
