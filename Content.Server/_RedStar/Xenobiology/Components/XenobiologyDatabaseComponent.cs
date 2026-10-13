using Content.Shared._RedStar.Xenobiology;

namespace Content.Server._RedStar.Xenobiology.Components;

/// <summary>
/// Stores physical samples on a research server.
/// </summary>
[RegisterComponent]
public sealed partial class XenobiologyDatabaseComponent : Component
{
    [DataField]
    public List<CellEntry> Cells = [];

    [DataField]
    public int NextCellId = 1;
}
