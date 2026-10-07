using Robust.Shared.Prototypes;

namespace Content.Shared._RedStar.Xenobiology.Components.Container;

/// <summary>
/// Tracks permanent traits acquired from injected cells without retaining the samples.
/// </summary>
[RegisterComponent]
public sealed partial class CellTraitHostComponent : Component
{
    [ViewVariables]
    public readonly HashSet<ProtoId<CellTraitPrototype>> AcquiredTraits = [];
}
