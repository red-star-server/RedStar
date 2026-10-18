using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared._RedStar.Genetics.Components;

/// <summary>
/// Handheld scanner that maintains a live genetic report while its patient remains within range.
/// Patient data is sent only through the scanner's user interface.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class GeneticAnalyzerComponent : Component
{
    [DataField]
    public TimeSpan ScanDelay = TimeSpan.FromSeconds(1.5);

    [DataField]
    public float MaxScanRange = 2.5f;

    [DataField]
    public TimeSpan UpdateInterval = TimeSpan.FromSeconds(1);

    [DataField]
    public SoundSpecifier ScanningEndSound = new SoundPathSpecifier("/Audio/Items/Medical/healthscanner.ogg");

    /// <summary>
    /// Patient followed by the server until the scanner is dropped, stored, or used on another subject.
    /// </summary>
    [ViewVariables]
    public EntityUid? Target;

    [ViewVariables]
    public TimeSpan NextUpdate;
}
