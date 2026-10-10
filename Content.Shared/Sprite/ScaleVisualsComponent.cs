using System.Numerics;
using Robust.Shared.GameStates;

namespace Content.Shared.Sprite;

/// <summary>
/// Used to set the <see cref="Robust.Client.GameObjects.SpriteComponent.Scale"/> datafield to a certain value from the server.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedScaleVisualsSystem))]
public sealed partial class ScaleVisualsComponent : Component
{
    /// <summary>
    /// The current sprite scale.
    /// </summary>
    [DataField, AutoNetworkedField]
    [ViewVariables]
    public Vector2 Scale = Vector2.One;

    /// <summary>
    /// The original sprite scale, which we revert to if this component is removed.
    /// Only set on the client.
    /// </summary>
    [DataField]
    [ViewVariables]
    public Vector2? OriginalScale;

    // RS14-start
    [DataField, AutoNetworkedField]
    public Vector2 SpeciesScale = Vector2.One;

    [DataField, AutoNetworkedField]
    public Vector2 ProfileScale = Vector2.One;

    [DataField, AutoNetworkedField]
    public Vector2 ComputedScale = Vector2.One;
    // RS14-end
}
