using Content.Shared._RedStar.Beekeeping.Systems;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared._RedStar.Beekeeping.Components;

/// <summary>
/// A handheld scanner for a hive, a queen or a bee in hand. Works like the plant analyzer: a short scan, then a
/// window that keeps itself up to date while the user stays next to the target.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedApiaryAnalyzerSystem))]
public sealed partial class ApiaryAnalyzerComponent : Component
{
    [DataField]
    public TimeSpan ScanDelay = TimeSpan.FromSeconds(0.5);

    [DataField]
    public SoundSpecifier ScanningEndSound = new SoundPathSpecifier("/Audio/Items/Medical/healthscanner.ogg");

    /// <summary>
    /// The hive, queen or bee being watched.
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public EntityUid? Target;

    /// <summary>
    /// Who scanned it and has the window open.
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public EntityUid? User;
}
