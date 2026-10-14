using Robust.Shared.GameStates;
using Robust.Shared.Audio;

namespace Content.Shared._RedStar.Xenobiology.Components.Machines;

/// <summary>
/// Injects genetic material from a vial into a specimen.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentPause]
public sealed partial class CellMutagenicInjectorComponent : Component
{
    [DataField]
    public string VialSlot = "vialSlot";

    [DataField]
    public TimeSpan InjectionDelay = TimeSpan.FromSeconds(5);

    [DataField]
    public SoundSpecifier? ProcessSound;

    [ViewVariables, AutoPausedField]
    public TimeSpan? InjectionEndTime;

    [ViewVariables]
    public EntityUid? InjectionSubject;

    [ViewVariables]
    public EntityUid? InjectionVial;

    [ViewVariables]
    public int InjectionVialRevision;

    public EntityUid? AudioStream;
}
