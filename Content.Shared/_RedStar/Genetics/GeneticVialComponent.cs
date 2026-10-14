using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._RedStar.Genetics;

/// <summary>
/// Physical storage for extracted gene values. Only GeneticVialSystem changes its contents.
/// </summary>
[RegisterComponent]
public sealed partial class GeneticVialComponent : Component
{
    [DataField]
    public Dictionary<ProtoId<GenePrototype>, GeneState> Genes = [];

    public int Revision;
}

[Serializable, NetSerializable]
public enum GeneticVialVisuals : byte
{
    Filled,
    Layer
}
