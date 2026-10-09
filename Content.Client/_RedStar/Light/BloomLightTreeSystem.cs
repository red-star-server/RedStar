using System.Numerics;
using Robust.Shared.ComponentTrees;
using Robust.Shared.Physics;

namespace Content.Client.Light;

public sealed partial class BloomLightTreeSystem : ComponentTreeSystem<BloomLightTreeComponent, BloomLightComponent>
{
    protected override bool DoFrameUpdate => true;
    protected override bool DoTickUpdate => false;
    protected override bool Recursive => true;

    protected override Box2 ExtractAabb(in ComponentTreeEntry<BloomLightComponent> entry, Vector2 pos, Angle rot)
    {
        // The cone texture spans four tiles and may be displaced from the fixture.
        var extent = entry.Component.ShowCone ? 3.5f : 1.5f;
        var half = new Vector2(extent);
        return new Box2(pos - half, pos + half);
    }
}
