using Content.Server._RedStar.Xenobiology.Systems;
using Robust.Shared.Audio;

namespace Content.Server._RedStar.Xenobiology.Components;

/// <summary>
/// Injects genetic material from a vial into a specimen.
/// </summary>
[RegisterComponent, AutoGenerateComponentPause]
[Access(typeof(MutagenicInjectorSystem))]
public sealed partial class MutagenicInjectorComponent : Component
{
    /// <summary>
    /// Item slot holding the vial to inject.
    /// </summary>
    [DataField]
    public string VialSlot = "vialSlot";

    /// <summary>
    /// Duration of one injection.
    /// </summary>
    [DataField]
    public TimeSpan InjectionDelay = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Sound played in a loop while injecting.
    /// </summary>
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
