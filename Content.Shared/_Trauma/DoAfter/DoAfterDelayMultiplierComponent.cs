using Robust.Shared.GameStates;

namespace Content.Shared._Trauma.DoAfter;

/// <summary>
/// Component for bodyparts that change doafter delay length.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class DoAfterDelayMultiplierComponent : Component
{
    /// <summary>
    /// Multiplier applied to do-after duration when relayed through this organ.
    /// </summary>
    [DataField(required: true)]
    public float Multiplier = 1f;
}
