using Robust.Shared.GameStates;

namespace Content.Shared._Trauma.Surgery.Steps.Parts;

/// <summary>
/// Tracks which surgical steps have been completed on a body part.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SurgeryStateComponent : Component
{
    [DataField, AutoNetworkedField]
    public SurgeryStepFlags CompletedSteps = SurgeryStepFlags.None;
}

[Flags]
public enum SurgeryStepFlags : ushort
{
    None = 0,
    IncisionOpen = 1 << 0,
    BleedersClamped = 1 << 1,
    SkinRetracted = 1 << 2,
    BonesSawed = 1 << 3,
    BonesOpen = 1 << 4,
    InternalBleedersClamped = 1 << 5,
    BodyPartSawed = 1 << 6,
    VesselsStitched = 1 << 7,
    TissueRearranged = 1 << 8,
    JointCreated = 1 << 9,
    JointStitched = 1 << 10
}
