using System.Numerics;
using Robust.Shared.ComponentTrees;
using Robust.Shared.Physics;
using Robust.Shared.Utility;

namespace Content.Shared.Light;

/// <summary>
/// Client-side mask for a fixture's point light.
/// </summary>
[RegisterComponent]
public sealed partial class BloomLightComponent : Component, IComponentTreeEntry<BloomLightComponent>
{
    [DataField]
    public bool Enabled = true;

    [DataField]
    public Color Tint = Color.White;

    [DataField]
    public List<BloomLightLayer> Layers = [];

    public EntityUid? TreeUid { get; set; }
    public DynamicTree<ComponentTreeEntry<BloomLightComponent>>? Tree { get; set; }
    public bool AddToTree => Enabled;
    public bool TreeUpdateQueued { get; set; }
}

/// <summary>
/// A local glow layer. Size and offset rotate with the fixture, independently of its light radius.
/// </summary>
[DataDefinition]
public sealed partial class BloomLightLayer
{
    [DataField(required: true)]
    public SpriteSpecifier Mask = default!;

    [DataField]
    public Vector2 Offset;

    [DataField]
    public float Opacity = 0.05f;
}
