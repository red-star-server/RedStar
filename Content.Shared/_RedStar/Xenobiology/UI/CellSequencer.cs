using Robust.Shared.Serialization;

namespace Content.Shared._RedStar.Xenobiology.UI;

[Serializable, NetSerializable]
public enum CellSequencerUiKey
{
    Key
}

[Serializable, NetSerializable]
public sealed class SequencerMarkerView(
    string familyCode,
    string[] fragments,
    bool decoded,
    string name,
    string value,
    string sequence)
{
    public readonly string FamilyCode = familyCode;
    public readonly string[] Fragments = fragments;
    public readonly bool Decoded = decoded;
    public readonly string Name = name;
    public readonly string Value = value;
    public readonly string Sequence = sequence;
}

[Serializable, NetSerializable]
public sealed class SequencerSampleView(CellView cell, SequencerMarkerView[] markers)
{
    public readonly CellView Cell = cell;
    public readonly SequencerMarkerView[] Markers = markers;
}

[Serializable, NetSerializable]
public sealed class CellSequencerUiState(SequencerSampleView[] samples, int dishRevision, NetEntity? dish,
    double retrySeconds)
    : BoundUserInterfaceState
{
    public readonly SequencerSampleView[] Samples = samples;
    public readonly int DishRevision = dishRevision;
    public readonly NetEntity? Dish = dish;
    public readonly double RetrySeconds = retrySeconds;
}

[Serializable, NetSerializable]
public sealed class CellSequencerDecodeMessage(int sampleIndex, int markerIndex, int[] order,
    int dishRevision, NetEntity? dish) : BoundUserInterfaceMessage
{
    public readonly int SampleIndex = sampleIndex;
    public readonly int MarkerIndex = markerIndex;
    public readonly int[] Order = order;
    public readonly int DishRevision = dishRevision;
    public readonly NetEntity? Dish = dish;
}
