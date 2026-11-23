using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared._RedStar.Genetics.Components;

/// <summary>
/// Handheld scanner that maintains a live genetic report while its patient remains within range.
/// Patient data is sent only through the scanner's user interface.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentPause]
public sealed partial class GeneticAnalyzerComponent : Component
{
    /// <summary>
    /// Time needed to scan a target.
    /// </summary>
    [DataField]
    public TimeSpan ScanDelay = TimeSpan.FromSeconds(1.5);

    /// <summary>
    /// Distance beyond which the report of a scanned target pauses.
    /// </summary>
    [DataField]
    public float MaxScanRange = 2.5f;

    /// <summary>
    /// Interval between report refreshes while a target is tracked.
    /// </summary>
    [DataField]
    public TimeSpan UpdateInterval = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Sound played when a scan finishes.
    /// </summary>
    [DataField]
    public SoundSpecifier ScanningEndSound = new SoundPathSpecifier("/Audio/Items/Medical/healthscanner.ogg");

    /// <summary>
    /// Patient followed by the server until the scanner is dropped, stored, or used on another subject.
    /// </summary>
    [ViewVariables]
    public EntityUid? Target;

    [ViewVariables, AutoPausedField]
    public TimeSpan NextUpdate;
}
