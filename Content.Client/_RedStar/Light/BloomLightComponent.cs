using System.Numerics;
using Robust.Shared.ComponentTrees;
using Robust.Shared.Physics;
using Robust.Shared.Utility;

namespace Content.Client.Light;

/// <summary>
/// Client-side halo masks for a fixture's point light.
/// </summary>
[RegisterComponent]
public sealed partial class BloomLightComponent : Component, IComponentTreeEntry<BloomLightComponent>
{
    [DataField]
    public SpriteSpecifier PointMask = new SpriteSpecifier.Rsi(new ResPath("_RedStar/Effects/LightMasks/64.rsi"), "light_point");

    [DataField]
    public SpriteSpecifier ConeMask = new SpriteSpecifier.Rsi(new ResPath("_RedStar/Effects/LightMasks/128.rsi"), "light_cone");

    [DataField]
    public Vector2 PointOffset = new(0f, 0.45f);

    [DataField]
    public Vector2 ConeOffset = new(0f, -0.2f);

    [DataField]
    public bool ShowCone = true;

    [DataField]
    public bool Enabled = true;

    public EntityUid? TreeUid { get; set; }
    public DynamicTree<ComponentTreeEntry<BloomLightComponent>>? Tree { get; set; }
    public bool AddToTree => Enabled;
    public bool TreeUpdateQueued { get; set; }
}
