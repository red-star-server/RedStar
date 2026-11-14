using Content.Shared.Body;
using Content.Shared.Tools;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Shared._Trauma.Surgery.Graph;

/// <summary>
/// How a procedure's organ parameter is resolved when expanding it into concrete instances.
/// </summary>
public enum SurgeryOrganMode : byte
{
    /// <summary>Not organ-specific.</summary>
    None,
    /// <summary>One instance per organ present on the part; the player picks via UI (later: organ click).</summary>
    Focus,
    /// <summary>One instance per organ slot of the part; the step requires an organ of that category in hand.</summary>
    HeldItem,
}

/// <summary>
/// A single surgery procedure: an edge between two stages with tool-driven steps.
/// </summary>
[Prototype]
public sealed partial class SurgeryProcedurePrototype : IPrototype
{
    [ViewVariables]
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public LocId Name;

    [DataField(required: true)]
    public List<ProtoId<SurgeryStagePrototype>> From = [];

    [DataField(required: true)]
    public ProtoId<SurgeryStagePrototype> To = default!;

    [DataField]
    public SurgeryOrganMode OrganMode;

    /// <summary>
    /// Ordering in the surgery UI.
    /// </summary>
    [DataField]
    public int Priority;

    [DataField]
    public List<SurgeryStepCondition> Conditions = [];

    [DataField(required: true)]
    public List<SurgeryStepEntry> Steps = [];

    /// <summary>
    /// Applied once when every step has completed, before moving to the target stage.
    /// </summary>
    [DataField]
    public List<SurgeryStepEffect> Effects = [];
}

[DataDefinition]
public sealed partial class SurgeryStepEntry
{
    /// <summary>
    /// Localization key (or raw text fallback) shown in the UI.
    /// </summary>
    [DataField(required: true)]
    public string Name = default!;

    [DataField]
    public ProtoId<ToolQualityPrototype>? Quality;

    [DataField]
    public ProtoId<OrganCategoryPrototype>? Organ;

    /// <summary>
    /// Match the organ category of the procedure instance instead of a fixed one.
    /// </summary>
    [DataField]
    public bool ProcedureOrgan;

    [DataField]
    public bool AnyItem;

    [DataField]
    public TimeSpan Duration = TimeSpan.FromSeconds(2);

    [DataField]
    public SoundSpecifier? Sound;

    /// <summary>
    /// Consume the held item on completion (one charge for stacks, the whole item otherwise).
    /// </summary>
    [DataField]
    public bool Consume;

    [DataField]
    public List<SurgeryStepEffect> Effects = [];
}