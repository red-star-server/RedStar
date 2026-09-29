using System.Numerics;

namespace Content.Client._RedStar.Emoting.Components;

[RegisterComponent]
public sealed partial class ActiveEmoteAnimationComponent : Component
{
    public Vector2 StartOffset;

    public Angle StartRotation;
}
