using Robust.Shared.Serialization;

namespace Content.Shared._RedStar.Xenobiology.UI;

[Serializable, NetSerializable]
public enum CellSequencerUiKey { Key }

[Serializable, NetSerializable]
public sealed record SequencerMarkerView(string FamilyCode, string[] Fragments, bool Decoded,
    string Name, bool Extractable);

[Serializable, NetSerializable]
public sealed class SequencerSampleView(SampleView sample, SequencerMarkerView[] markers)
{
    public readonly SampleView Sample = sample;
    public readonly SequencerMarkerView[] Markers = markers;
}

[Serializable, NetSerializable]
public sealed class CellSequencerUiState(SequencerSampleView[] samples, NetEntity? dish,
    NetEntity? vial) : BoundUserInterfaceState
{
    public readonly SequencerSampleView[] Samples = samples;
    public readonly NetEntity? Dish = dish;
    public readonly NetEntity? Vial = vial;
}

/// <summary>
/// Feedback for one submitted assembly, sent only to the submitting user.
/// </summary>
[Serializable, NetSerializable]
public sealed class CellSequencerFeedbackMessage(int sampleId, string family, int[] order, bool[] matches)
    : BoundUserInterfaceMessage
{
    public readonly int SampleId = sampleId;
    public readonly string Family = family;
    public readonly int[] Order = order;
    public readonly bool[] Matches = matches;
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
