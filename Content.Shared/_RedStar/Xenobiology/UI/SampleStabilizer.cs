using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._RedStar.Xenobiology.UI;

[Serializable, NetSerializable]
public enum SampleStabilizerUiKey
{
    Key
}

[Serializable, NetSerializable]
public sealed class SampleStabilizerUiStabilizationMessage(int sampleId) : BoundUserInterfaceMessage
{
    public readonly int SampleId = sampleId;
}

/// <summary>
/// Samples of the inserted dish and the reagent supply of the inserted beaker.
/// StabilityPerUnit and MaximumGain let the client show the cost of stabilizing a sample.
/// </summary>
[Serializable, NetSerializable]
public sealed class SampleStabilizerUiState(
    SampleView[] samples,
    NetEntity? dish,
    bool beakerInserted,
    ProtoId<ReagentPrototype> reagent,
    FixedPoint2 reagentAmount,
    float stabilityPerUnit,
    float maximumGain,
    bool stabilizationInProgress,
    double stabilizationDuration) : BoundUserInterfaceState
{
    public readonly SampleView[] Samples = samples;
    public readonly NetEntity? Dish = dish;
    public readonly bool BeakerInserted = beakerInserted;
    public readonly ProtoId<ReagentPrototype> Reagent = reagent;
    public readonly FixedPoint2 ReagentAmount = reagentAmount;
    public readonly float StabilityPerUnit = stabilityPerUnit;
    public readonly float MaximumGain = maximumGain;
    public readonly bool StabilizationInProgress = stabilizationInProgress;
    public readonly double StabilizationDuration = stabilizationDuration;
}
