using Content.Shared.Body;
using Content.Shared.Tools;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Shared._Trauma.Surgery.Graph;

[Prototype]
public sealed partial class SurgeryGraphPrototype : IPrototype
{
    [ViewVariables]
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField]
    public string Name = string.Empty;

    [DataField(required: true)]
    public string StartNode = default!;

    [DataField("nodes")]
    public List<SurgeryGraphNode> Nodes = [];
}

[DataDefinition]
public sealed partial class SurgeryGraphNode
{
    [DataField(required: true)]
    public string Name = default!;

    [DataField]
    public string Label = string.Empty;

    [DataField]
    public List<SurgeryGraphEdge> Edges = [];

    [DataField]
    public List<ProtoId<SurgeryEdgePackagePrototype>> Packages = [];
}

[DataDefinition]
public sealed partial class SurgeryGraphEdge
{
    [DataField(required: true)]
    public string Id = default!;

    [DataField(required: true)]
    public string Target = default!;

    [DataField]
    public string Label = string.Empty;

    [DataField]
    public List<SurgeryStepEntry> Steps = [];

    [DataField]
    public List<SurgeryStepCondition> Conditions = [];

    [DataField]
    public List<SurgeryStepEffect> Effects = [];
}

[Prototype]
public sealed partial class SurgeryEdgePackagePrototype : IPrototype
{
    [ViewVariables]
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField("edges", required: true)]
    public List<SurgeryGraphEdge> Edges = [];
}

[DataDefinition]
public sealed partial class SurgeryStepEntry
{
    [DataField(required: true)]
    public string Name = default!;

    [DataField]
    public ProtoId<ToolQualityPrototype>? Quality;

    [DataField]
    public ProtoId<OrganCategoryPrototype>? Organ;

    [DataField]
    public bool AnyItem;

    [DataField]
    public TimeSpan Duration = TimeSpan.FromSeconds(2);

    [DataField]
    public SoundSpecifier? Sound;

    [DataField]
    public List<SurgeryStepEffect> Effects = [];
}
