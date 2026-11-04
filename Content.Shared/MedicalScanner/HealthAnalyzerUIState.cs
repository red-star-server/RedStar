// RS14-start
using Content.Shared._Trauma.Common.Wounds;
using Content.Shared.Body;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;
// RS14-end
using Robust.Shared.Serialization;

namespace Content.Shared.MedicalScanner;

/// <summary>
/// Contains the current state of a health analyzer control. Used for the health analyzer and cryo pod.
/// </summary>
[Serializable, NetSerializable]
public sealed class HealthAnalyzerUiState : BoundUserInterfaceState
{
    public readonly NetEntity? TargetEntity;
    public float Temperature;
    public float BloodLevel;
    public bool ScanMode;
    // RS14-start
    public HashSet<ProtoId<OrganCategoryPrototype>> Bleeding = new();
    public Dictionary<ProtoId<OrganCategoryPrototype>, WoundableSeverity>? Body;
    public FixedPoint2 VitalDamage;
    public NetEntity? Part;
    // RS14-end
    public bool Unrevivable;

    public HealthAnalyzerUiState() {}

    public HealthAnalyzerUiState(NetEntity? targetEntity, float temperature, float bloodLevel, bool scanMode, bool bleeding, bool unrevivable)
    {
        TargetEntity = targetEntity;
        Temperature = temperature;
        BloodLevel = bloodLevel;
        ScanMode = scanMode;
        // RS14-start
        // Legacy callers only have a global bleeding flag.
        if (bleeding)
            Bleeding.Add("Torso");
        // RS14-end
        Unrevivable = unrevivable;
    }
}
