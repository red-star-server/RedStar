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
    public SpriteSpecifier Mask = new SpriteSpecifier.Rsi(new ResPath("_RedStar/Effects/LightMasks/128.rsi"), "light_cone");

    [DataField]
    public Vector2 MaskOffset = new(0f, -0.2f);

    [DataField]
    public float MaskOpacity = 0.25f;

    public EntityUid? TreeUid { get; set; }
    public DynamicTree<ComponentTreeEntry<BloomLightComponent>>? Tree { get; set; }
    public bool AddToTree => true;
    public bool TreeUpdateQueued { get; set; }
}
