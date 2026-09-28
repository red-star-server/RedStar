namespace Content.Server._RedStar.Xenobiology;

/// <summary>
/// A local specimen detector. Detection is requested by an open linked console;
/// the scanner does not retain specimen lists or analysis data.
/// </summary>
[RegisterComponent]
public sealed partial class XenobiologyCellScannerComponent : Component
{
    [DataField]
    public float DetectionRadius = 4f;

    [DataField]
    public TimeSpan UpdateInterval = TimeSpan.FromSeconds(0.75);
}
