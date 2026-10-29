using Content.Shared.Body;
using Content.Shared.Tools;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Trauma.Surgery.Steps;

[RegisterComponent, NetworkedComponent]
[EntityCategory("SurgerySteps")]
public sealed partial class SurgeryStepComponent : Component
{
    /// <summary>
    /// Surgical tool components accepted by this step.
    /// </summary>
    [DataField]
    public ComponentRegistry? Tool;

    /// <summary>
    /// Tool qualities accepted by this step.
    /// </summary>
    [DataField]
    public HashSet<ProtoId<ToolQualityPrototype>>? ToolQualities;

    /// <summary>
    /// Components added to the selected part when the step completes.
    /// </summary>
    [DataField]
    public ComponentRegistry? Add;

    /// <summary>
    /// Components added to the patient's body when the step completes.
    /// </summary>
    [DataField]
    public ComponentRegistry? BodyAdd;

    /// <summary>
    /// Components removed from the selected part when the step completes.
    /// </summary>
    [DataField]
    public ComponentRegistry? Remove;

    /// <summary>
    /// Components removed from the patient's body when the step completes.
    /// </summary>
    [DataField]
    public ComponentRegistry? BodyRemove;

    /// <summary>
    ///   These components will be added to the body part's organs' OnAdd field.
    ///   Each key is the SlotId of the organ to look for.
    ///
    ///   Used to make organs add components to whatever body it's residing in.
    /// </summary>
    [DataField]
    public Dictionary<ProtoId<OrganCategoryPrototype>, ComponentRegistry>? AddOrganOnAdd;

    /// <summary>
    ///   These components will be removed from the body part's organs' OnAdd field.
    ///   Each key is the SlotId of the organ to look for.
    ///
    ///   Used to stop organs from adding components to whatever body it's residing in.
    /// </summary>
    [DataField]
    public Dictionary<ProtoId<OrganCategoryPrototype>, ComponentRegistry>? RemoveOrganOnAdd;

    /// <summary>
    /// Base duration of the step before surgery speed modifiers.
    /// </summary>
    [DataField]
    public TimeSpan Duration = TimeSpan.FromSeconds(2);
}
