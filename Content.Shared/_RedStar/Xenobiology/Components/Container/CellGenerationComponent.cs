using Content.Shared._RedStar.Genetics;
using Content.Shared._RedStar.Xenobiology.Systems;
using Robust.Shared.Prototypes;

namespace Content.Shared._RedStar.Xenobiology.Components.Container;

/// <summary>
/// Generates the configured samples once when a container is created.
/// Appearance derives from generated genes; stability belongs to the sample.
/// </summary>
/// <seealso cref="SharedCellSystem"/>
[RegisterComponent]
public sealed partial class CellGenerationComponent : Component
{
    [DataField]
    public List<CellGenerationEntry> Cells = [];
}

[DataDefinition]
public sealed partial class CellGenerationEntry
{
    [DataField(required: true)]
    public ProtoId<GenomePrototype> Genome;

    [DataField]
    public float Stability = 1f;
}
