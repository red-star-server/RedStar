using Robust.Shared.GameStates;

namespace Content.Shared._Trauma.Surgery.Effects.Step;

[RegisterComponent, NetworkedComponent]
public sealed partial class SurgeryStepCavityEffectComponent : Component
{
    /// <summary>
    /// Action identifier for inserting an item into the cavity.
    /// </summary>
    public const string InsertAction = "Insert";

    /// <summary>
    /// Action identifier for removing an item from the cavity.
    /// </summary>
    public const string RemoveAction = "Remove";

    /// <summary>
    /// Whether the step inserts an item into the cavity or removes it.
    /// </summary>
    [DataField]
    public string Action = InsertAction;
}
