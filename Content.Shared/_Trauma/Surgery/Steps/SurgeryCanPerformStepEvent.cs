using Content.Shared._Trauma.Common.Surgery.Tools;
using Content.Shared.Inventory;

namespace Content.Shared._Trauma.Surgery.Steps;

[ByRefEvent]
public record struct SurgeryCanPerformStepEvent(
    EntityUid User,
    EntityUid Body,
    EntityUid Tool,
    SlotFlags TargetSlots,
    string? Popup = null,
    StepInvalidReason Invalid = StepInvalidReason.None,
    BaseSurgeryToolComponent? ValidTool = null
) : IInventoryRelayEvent
{
    public bool IsValid => Invalid == StepInvalidReason.None;
    public bool IsInvalid => !IsValid;
}
