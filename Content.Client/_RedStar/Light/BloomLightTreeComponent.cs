using Content.Shared.Light;
using Robust.Shared.ComponentTrees;
using Robust.Shared.Physics;

namespace Content.Client.Light;

[RegisterComponent]
public sealed partial class BloomLightTreeComponent : Component, IComponentTreeComponent<BloomLightComponent>
{
    public DynamicTree<ComponentTreeEntry<BloomLightComponent>> Tree { get; set; } = default!;
}
