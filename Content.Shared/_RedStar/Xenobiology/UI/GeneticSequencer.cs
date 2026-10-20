using Robust.Shared.Serialization;

namespace Content.Shared._RedStar.Xenobiology.UI;

[Serializable, NetSerializable]
public enum GeneticSequencerUiKey { Key }

/// <summary>
/// Sequencing puzzle and discovery status without the gene prototype ID or solution order.
/// FamilyCode identifies this gene's puzzle for the current round.
/// </summary>
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
public sealed class GeneticSequencerUiState(SequencerSampleView[] samples, NetEntity? dish,
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
public sealed class GeneticSequencerFeedbackMessage(int sampleId, string family, int[] order, bool[] matches)
    : BoundUserInterfaceMessage
{
    public readonly int SampleId = sampleId;
    public readonly string Family = family;
    /// <summary>
    /// Fragment indices in submitted board order, used to reject feedback for a changed assembly.
    /// </summary>
    public readonly int[] Order = order;
    /// <summary>
    /// Whether each adjacent pair overlaps correctly; contains one entry fewer than Order.
    /// </summary>
    public readonly bool[] Matches = matches;
}

[Serializable, NetSerializable]
public sealed class GeneticSequencerDecodeMessage(int sampleId, string family, int[] order) : BoundUserInterfaceMessage
{
    public readonly int SampleId = sampleId;
    public readonly string Family = family;
    public readonly int[] Order = order;
}

[Serializable, NetSerializable]
public sealed class GeneticSequencerExtractMessage(int sampleId, string family, NetEntity? vial) : BoundUserInterfaceMessage
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
