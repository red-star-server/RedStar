using Content.Shared._RedStar.Genetics.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Server._RedStar.Genetics.Components;

/// <summary>
/// Tracks shared resources required by active genes and whether genetics created them.
/// Runtime component references are resolved again after loading, never copied to a clone.
/// </summary>
[RegisterComponent]
public sealed partial class GeneContributionsComponent : Component
{
    [DataField]
    public Dictionary<string, GeneComponentContribution> Components = new();

    [DataField]
    public Dictionary<string, GeneSolutionContribution> Solutions = new();

    [DataField]
    public Dictionary<EntProtoId, GeneActionContribution> Actions = new();
}

[DataDefinition]
public sealed partial class GeneComponentContribution
{
    [DataField]
    public HashSet<ProtoId<GenePrototype>> Sources = new();

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
    [DataField]
    public HashSet<ProtoId<GenePrototype>> Sources = new();

    /// <summary>
    /// Action created by genetics. Null means the host already had an independent action of this prototype.
    /// </summary>
    [DataField]
    public EntityUid? Owned;
}
