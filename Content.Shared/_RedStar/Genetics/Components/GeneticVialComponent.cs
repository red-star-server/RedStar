using Content.Shared._RedStar.Genetics.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Shared._RedStar.Genetics.Components;

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
