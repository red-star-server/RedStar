using Robust.Shared.Serialization;

namespace Content.Shared._RedStar.Xenobiology.UI;

[Serializable, NetSerializable]
public enum CellSequencerUiKey
{
    Key
}

[Serializable, NetSerializable]
public sealed class CellSequencerUiState(CellView[] insideCells) : BoundUserInterfaceState
{
    public readonly CellView[] InsideCells = insideCells;
}
