using Robust.Shared.Prototypes;

namespace Content.Shared._RedStar.Xenobiology.Components.Traits;

/// <summary>
/// Explicitly identifies biological traits that cannot be inferred from generic components.
/// </summary>
[RegisterComponent]
public sealed partial class CellTraitSourceComponent : Component
{
    [DataField]
    public List<ProtoId<CellTraitPrototype>> Traits = [];
}
