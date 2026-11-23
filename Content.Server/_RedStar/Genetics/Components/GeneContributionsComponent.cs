using Content.Server._RedStar.Genetics.Systems;
using Content.Shared._RedStar.Genetics.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Server._RedStar.Genetics.Components;

/// <summary>
/// Tracks shared resources required by active genes and whether genetics created them.
/// Runtime component references are resolved again after loading, never copied to a clone.
/// </summary>
[RegisterComponent]
[Access(typeof(ServerGenomeSystem), typeof(GeneExpressionSystem))]
public sealed partial class GeneContributionsComponent : Component
{
    /// <summary>
    /// Components supplied by acquired genes, keyed by registered component name.
    /// </summary>
    [DataField]
    public Dictionary<string, GeneComponentContribution> Components = new();

    /// <summary>
    /// Solutions supplied by acquired genes, keyed by solution name.
    /// </summary>
    [DataField]
    public Dictionary<string, GeneSolutionContribution> Solutions = new();

    /// <summary>
    /// Actions supplied by acquired genes, keyed by action prototype.
    /// </summary>
    [DataField]
    public Dictionary<EntProtoId, GeneActionContribution> Actions = new();
}

[DataDefinition]
public sealed partial class GeneComponentContribution
{
    /// <summary>
    /// Expressed genes that currently require this contribution. It is withdrawn when the last source stops.
    /// </summary>
    [DataField]
    public HashSet<ProtoId<GenePrototype>> Sources = new();

    /// <summary>
    /// Whether genetics added the component, so that it may remove it.
    /// </summary>
    [DataField]
    public bool Owned;

    /// <summary>
    /// Exact component supplied by genetics; a replacement supplied by another system is not removed.
    /// </summary>
    public IComponent? Instance;
}

[DataDefinition]
public sealed partial class GeneSolutionContribution
{
    /// <summary>
    /// Expressed genes that currently require this contribution. It is withdrawn when the last source stops.
    /// </summary>
    [DataField]
    public HashSet<ProtoId<GenePrototype>> Sources = new();

    /// <summary>
    /// Solution created by genetics. Null means the host already had its own solution.
    /// </summary>
    [DataField]
    public EntityUid? Owned;
}

[DataDefinition]
public sealed partial class GeneActionContribution
{
    /// <summary>
    /// Expressed genes that currently require this contribution. It is withdrawn when the last source stops.
    /// </summary>
    [DataField]
    public HashSet<ProtoId<GenePrototype>> Sources = new();

    /// <summary>
    /// Action created by genetics. Null means the host already had an independent action of this prototype.
    /// </summary>
    [DataField]
    public EntityUid? Owned;
}
