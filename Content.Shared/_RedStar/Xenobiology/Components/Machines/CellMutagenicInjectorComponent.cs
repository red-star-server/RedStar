using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Audio;

namespace Content.Shared._RedStar.Xenobiology.Components.Machines;

/// <summary>
/// Injects cells from a Petri dish into a specimen.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentPause]
public sealed partial class CellMutagenicInjectorComponent : Component
{
    public const string BodyContainerId = "injector-bodyContainer";

    [DataField]
    public string DishSlot = "dishSlot";

    [DataField]
    public TimeSpan InjectionDelay = TimeSpan.FromSeconds(5);

    [DataField]
    public SoundSpecifier? ProcessSound;

    [ViewVariables, AutoPausedField]
    public TimeSpan? InjectionEndTime;

    [ViewVariables]
    public EntityUid? InjectionSubject;

    [ViewVariables]
    public EntityUid? InjectionDish;

    [ViewVariables]
    public int InjectionDishRevision;

    public EntityUid? AudioStream;

    [ViewVariables]
    public ContainerSlot BodyContainer = default!;
}
