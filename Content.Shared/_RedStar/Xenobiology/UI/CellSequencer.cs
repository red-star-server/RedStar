using Robust.Shared.Serialization;

namespace Content.Shared._RedStar.Xenobiology.UI;

[Serializable, NetSerializable]
public enum CellSequencerUiKey { Key }

[Serializable, NetSerializable]
public sealed class SequencerMarkerView(string familyCode, string[] fragments, bool decoded,
    string name, bool extractable)
{
    public readonly string FamilyCode = familyCode;
    public readonly string[] Fragments = fragments;
    public readonly bool Decoded = decoded;
    public readonly string Name = name;
    public readonly bool Extractable = extractable;
}

[Serializable, NetSerializable]
public sealed class SequencerSampleView(SampleView sample, SequencerMarkerView[] markers)
{
    public readonly SampleView Sample = sample;
    public readonly SequencerMarkerView[] Markers = markers;
}

[Serializable, NetSerializable]
public sealed class CellSequencerUiState(SequencerSampleView[] samples, NetEntity? dish,
    int feedbackSample, string feedbackFamily, int[] feedbackOrder, NetEntity? vial) : BoundUserInterfaceState
{
    public readonly SequencerSampleView[] Samples = samples;
    public readonly NetEntity? Dish = dish;
    public readonly int FeedbackSample = feedbackSample;
    public readonly string FeedbackFamily = feedbackFamily;
    public readonly int[] FeedbackOrder = feedbackOrder;
    public readonly NetEntity? Vial = vial;
}

[Serializable, NetSerializable]
public sealed class CellSequencerDecodeMessage(int sampleId, string family, int[] order) : BoundUserInterfaceMessage
{
    public readonly int SampleId = sampleId;
    public readonly string Family = family;
    public readonly int[] Order = order;
}

[Serializable, NetSerializable]
public sealed class CellSequencerExtractMessage(int sampleId, string family, NetEntity? vial) : BoundUserInterfaceMessage
{
    public readonly int SampleId = sampleId;
    public readonly string Family = family;
    public readonly NetEntity? Vial = vial;
}

public static class SequencerPuzzle
{
    public const int Overlap = 4;
    public const int Step = 3;
    public const int ReadLength = Overlap + Step;
    public const int FamilyLength = 8;
}
