using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._RedStar.Genetics;

/// <summary>
/// Physical storage for extracted gene values. Only GeneticVialSystem changes its contents.
/// </summary>
[RegisterComponent]
public sealed partial class GeneticVialComponent : Component
{
    /// <summary>
    /// Extracted genes available for injection. Modified only through GeneticVialSystem.
    /// </summary>
    [DataField]
    public HashSet<ProtoId<GenePrototype>> Genes = [];

    /// <summary>
    /// Changes whenever vial contents change. An injection is cancelled if its captured revision differs.
    /// </summary>
    public int Revision;
}

[Serializable, NetSerializable]
public enum GeneticVialVisuals : byte
{
    Filled,
    Layer
}
