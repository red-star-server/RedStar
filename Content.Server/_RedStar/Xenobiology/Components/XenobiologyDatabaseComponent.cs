using Content.Shared._RedStar.Xenobiology;

namespace Content.Server._RedStar.Xenobiology.Components;

/// <summary>
/// The genome library hosted by a research server. Sent to clients through machine UI state.
/// </summary>
[RegisterComponent]
public sealed partial class XenobiologyDatabaseComponent : Component
{
    [DataField]
    public List<CellEntry> Cells = [];

    [DataField]
    public int NextCellId = 1;
}
