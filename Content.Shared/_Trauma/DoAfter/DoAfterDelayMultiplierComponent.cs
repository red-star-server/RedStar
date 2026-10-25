using Robust.Shared.GameStates;
namespace Content.Shared._Trauma.DoAfter;

/// <summary>
/// Component for bodyparts that change doafter delay length.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class DoAfterDelayMultiplierComponent : Component
{
    [DataField(required: true)]
    public float Multiplier = 1f;
}
