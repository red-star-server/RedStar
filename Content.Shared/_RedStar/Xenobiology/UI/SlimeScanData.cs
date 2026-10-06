using Content.Shared._RedStar.Xenobiology.Slimes;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._RedStar.Xenobiology.UI;

[Serializable, NetSerializable]
public readonly record struct SlimeScanData(
    string TargetName,
    float Growth,
    float? Hunger,
    SlimeMutationScanEntry[] Mutations,
    EntProtoId? Prototype,
    SlimeStage Stage);

[Serializable, NetSerializable]
public readonly record struct SlimeMutationScanEntry(EntProtoId Target, float Progress);
