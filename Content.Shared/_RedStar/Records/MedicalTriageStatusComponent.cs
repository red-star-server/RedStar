using Robust.Shared.GameStates;

namespace Content.Shared._RedStar.Records;

/// <summary>
/// Replicates a patient's station triage entry for medical HUDs.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class MedicalTriageStatusComponent : Component
{
    [AutoNetworkedField]
    public MedicalTriagePriority Priority;
}
