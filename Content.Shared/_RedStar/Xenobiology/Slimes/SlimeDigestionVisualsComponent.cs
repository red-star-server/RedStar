using System.Numerics;
using Robust.Shared.GameStates;

namespace Content.Shared._RedStar.Xenobiology.Slimes;

/// <summary>
/// Configures the space occupied by the contained victim's decorative sprite.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class SlimeDigestionVisualsComponent : Component
{
    public const string ContainerId = "slime-stomach";

    [DataField]
    public Vector2 InteriorSize = new(0.65f, 0.55f);

    [DataField]
    public Vector2 InteriorOffset = new(0, -0.1f);
}
