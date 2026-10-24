using Content.Shared.Chemistry.Reagent;
using Robust.Shared.Prototypes;

namespace Content.Shared._RedStar.Beekeeping.Components;

/// <summary>
/// A honeycomb whose sprite is grey overlays tinted with the color of what is in it. Plain honey gives each layer its
/// own base color; the more of other reagents, the closer each layer moves to their color. The server sets
/// <see cref="BeeCombVisuals.Color"/> and <see cref="BeeCombVisuals.Share"/>.
/// </summary>
[RegisterComponent]
public sealed partial class BeeCombVisualsComponent : Component
{
    /// <summary>
    /// Solution whose reagents give the color.
    /// </summary>
    [DataField]
    public string Solution = "food";

    /// <summary>
    /// What plain honey looks like on each tinted layer: the tint that gives the colors of the original icon on top
    /// of the layer's grey.
    /// </summary>
    [DataField]
    public Color CellsColor = Color.FromHex("#FFAC30");

    [DataField]
    public Color DripsColor = Color.FromHex("#EB8423");

    [DataField]
    public Color OutlineColor = Color.FromHex("#FFB70A");

    /// <summary>
    /// Reagents that make up the honey itself and so take the base colors of the layers.
    /// </summary>
    [DataField]
    public List<ProtoId<ReagentPrototype>> HoneyReagents = new() { "Honey", "Vitamin" };
}
