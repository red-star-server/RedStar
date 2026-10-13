using Content.Shared._RedStar.Xenobiology;
using Content.Shared._RedStar.Genetics;

namespace Content.Server._RedStar.Xenobiology.Components;

/// <summary>
/// Stores physical samples and the genetic material already studied on this research server.
/// </summary>
[RegisterComponent]
public sealed partial class XenobiologyDatabaseComponent : Component
{
    [DataField]
    public List<CellEntry> Cells = [];

    [DataField]
    public List<GenomeSnapshot> StudiedGenomes = [];

    [DataField]
    public int NextCellId = 1;
}
