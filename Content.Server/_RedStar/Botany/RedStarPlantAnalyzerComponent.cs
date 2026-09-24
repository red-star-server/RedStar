using Content.Shared.DoAfter;

namespace Content.Server._RedStar.Botany;

[RegisterComponent]
public sealed partial class RedStarPlantAnalyzerComponent : Component
{
    [DataField]
    public bool AdvancedScan;

    [DataField]
    public float ScanDelay = 0.5f;

    [DataField]
    public float AdvancedScanDelay = 1f;

    [ViewVariables]
    public DoAfterId? CurrentScan;
}
