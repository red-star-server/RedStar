using System.Numerics;
using Content.Shared.Light;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.ComponentTrees;
using Robust.Shared.Physics;

namespace Content.Client.Light;

public sealed partial class BloomLightTreeSystem : ComponentTreeSystem<BloomLightTreeComponent, BloomLightComponent>
{
    [Dependency] private SpriteSystem _sprites = default!;

    protected override bool DoFrameUpdate => true;
    protected override bool DoTickUpdate => false;
    protected override bool Recursive => true;

    protected override Box2 ExtractAabb(in ComponentTreeEntry<BloomLightComponent> entry, Vector2 pos, Angle rot)
    {
        var texture = _sprites.Frame0(entry.Component.Mask);
        var size = new Vector2(texture.Width, texture.Height) / EyeManager.PixelsPerMeter;
        var radius = size.Length() / 2f + entry.Component.MaskOffset.Length();
        var half = new Vector2(radius);
        return new Box2(pos - half, pos + half);
    }
}
