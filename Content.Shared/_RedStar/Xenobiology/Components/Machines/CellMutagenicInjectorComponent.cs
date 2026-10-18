using Robust.Shared.Audio;

namespace Content.Shared._RedStar.Xenobiology.Components.Machines;

/// <summary>
/// Injects genetic material from a vial into a specimen.
/// </summary>
[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class CellMutagenicInjectorComponent : Component
{
    [DataField]
    public string VialSlot = "vialSlot";

    [DataField]
    public TimeSpan InjectionDelay = TimeSpan.FromSeconds(5);

    [DataField]
    public SoundSpecifier? ProcessSound;

    /// <summary>
    /// Completion time in simulation time, shifted after a pause. Null while idle.
    /// </summary>
    [ViewVariables, AutoPausedField]
    public TimeSpan? InjectionEndTime;

    /// <summary>
    /// Recipient captured at startup. Replacing or removing it cancels the injection.
    /// </summary>
    [ViewVariables]
    public EntityUid? InjectionSubject;

    /// <summary>
    /// Vial captured at startup. Replacing or removing it cancels the injection.
    /// </summary>
    [ViewVariables]
    public EntityUid? InjectionVial;

    /// <summary>
    /// Vial contents revision captured at startup; a mismatch cancels the injection.
    /// </summary>
    [ViewVariables]
    public int InjectionVialRevision;

    public EntityUid? AudioStream;
}
