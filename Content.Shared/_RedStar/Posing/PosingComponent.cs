using System.Numerics;
using Robust.Shared.GameStates;

namespace Content.Shared._RedStar.Posing;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class PosingComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool Active;

    [DataField, AutoNetworkedField]
    public Vector2 Offset;

    [DataField, AutoNetworkedField]
    public Angle Rotation;

    [DataField]
    public Vector2 OffsetLimit = new(0.3f, 0.3f);

    [DataField]
    public float RotationLimit = 180f;
}
