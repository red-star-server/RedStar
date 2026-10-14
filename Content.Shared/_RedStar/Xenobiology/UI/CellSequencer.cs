using Robust.Shared.Serialization;

namespace Content.Shared._RedStar.Xenobiology.UI;

[Serializable, NetSerializable]
public enum CellSequencerUiKey { Key }

[Serializable, NetSerializable]
public sealed class SequencerMarkerView(string familyCode, string[] fragments, bool decoded,
    string name, string value, string sequence, bool extractable)
{
    public readonly string FamilyCode = familyCode;
    public readonly string[] Fragments = fragments;
    public readonly bool Decoded = decoded;
    public readonly string Name = name;
    public readonly string Value = value;
    public readonly string Sequence = sequence;
    public readonly bool Extractable = extractable;
}

[Serializable, NetSerializable]
public sealed class SequencerSampleView(SampleView sample, SequencerMarkerView[] markers)
{
    public readonly SampleView Sample = sample;
    public readonly SequencerMarkerView[] Markers = markers;
}

[Serializable, NetSerializable]
public sealed class CellSequencerUiState(SequencerSampleView[] samples, int dishRevision, NetEntity? dish,
    double retrySeconds, int feedbackSample, int feedbackMarker, int[] feedbackOrder,
    NetEntity? vial, int vialRevision) : BoundUserInterfaceState
{
    public readonly SequencerSampleView[] Samples = samples;
    public readonly int DishRevision = dishRevision;
    public readonly NetEntity? Dish = dish;
    public readonly double RetrySeconds = retrySeconds;
    public readonly int FeedbackSample = feedbackSample;
    public readonly int FeedbackMarker = feedbackMarker;
    public readonly int[] FeedbackOrder = feedbackOrder;
    public readonly NetEntity? Vial = vial;
    public readonly int VialRevision = vialRevision;
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

[Serializable, NetSerializable]
public sealed class CellSequencerExtractMessage(int sampleIndex, int markerIndex, int dishRevision,
    NetEntity? dish, NetEntity? vial, int vialRevision) : BoundUserInterfaceMessage
{
    public readonly int SampleIndex = sampleIndex;
    public readonly int MarkerIndex = markerIndex;
    public readonly int DishRevision = dishRevision;
    public readonly NetEntity? Dish = dish;
    public readonly NetEntity? Vial = vial;
    public readonly int VialRevision = vialRevision;
}
