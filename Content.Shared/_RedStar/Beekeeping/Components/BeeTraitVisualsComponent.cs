using Robust.Shared.GameStates;

namespace Content.Shared._RedStar.Beekeeping.Components;

/// <summary>
/// A queen or bee sprite that shows its colony's traits (<see cref="BeeTraitVisuals"/>): the layer mapped to
/// <see cref="BeeForagerVisualLayers.Grey"/> takes the payload colour, the one mapped to
/// <see cref="BeeForagerVisualLayers.Angry"/> shows for aggressive bees.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class BeeTraitVisualsComponent : Component
{
    /// <summary>
    /// Colour of the grey mask without a payload: honey.
    /// </summary>
    [DataField]
    public Color HoneyColor = Color.FromHex("#E79A3F");
}
