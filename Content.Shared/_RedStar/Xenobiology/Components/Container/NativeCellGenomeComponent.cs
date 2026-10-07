namespace Content.Shared._RedStar.Xenobiology.Components.Container;

/// <summary>
/// Keeps an organism's original traits and biopsy sample separate from injected cells.
/// </summary>
[RegisterComponent]
public sealed partial class NativeCellGenomeComponent : Component
{
    [ViewVariables]
    public Cell? Sample;
}
