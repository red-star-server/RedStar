using Robust.Shared.Serialization;
using Robust.Shared.Prototypes;

namespace Content.Shared._RedStar.Xenobiology;

[Serializable, NetSerializable]
public readonly record struct SlimeScanData(
    string TargetName,
    float Growth,
    float? Hunger,
    float MutationChance,
    EntProtoId[] PotentialMutations,
    bool ExtractYieldEnhanced,
    SlimeTemperament Temperament,
    SlimeCrowding Crowding,
    EntProtoId? Prototype,
    SlimeStage Stage);
