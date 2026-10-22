using Robust.Shared.Serialization;

namespace Content.Shared._RedStar.Xenobiology.UI;

[Serializable, NetSerializable]
public enum SampleStabilizerUiKey
{
    Key
}

[Serializable, NetSerializable]
public sealed class SampleStabilizerUiStabilizationMessage(int sampleAId, int sampleBId)
    : BoundUserInterfaceMessage
{
    public readonly int SampleAId = sampleAId;
    public readonly int SampleBId = sampleBId;
}

[Serializable, NetSerializable]
public sealed class SampleStabilizerUiState(
    SampleView[] samples,
    NetEntity? dish,
    bool stabilizationInProgress,
    double stabilizationDuration) : BoundUserInterfaceState
{
    public readonly SampleView[] Samples = samples;
    public readonly NetEntity? Dish = dish;
    public readonly bool StabilizationInProgress = stabilizationInProgress;
    public readonly double StabilizationDuration = stabilizationDuration;
}
