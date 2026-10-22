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
        var radius = 0f;
        foreach (var layer in entry.Component.Layers)
        {
            var texture = _sprites.Frame0(layer.Mask);
            var size = new Vector2(texture.Width, texture.Height) * layer.Scale / EyeManager.PixelsPerMeter;
            radius = Math.Max(radius, size.Length() / 2f + layer.Offset.Length());
        }

        var half = new Vector2(radius);
        return new Box2(pos - half, pos + half);
    }
}
