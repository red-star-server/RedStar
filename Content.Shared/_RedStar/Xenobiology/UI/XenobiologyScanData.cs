using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._RedStar.Xenobiology.UI;

[Serializable, NetSerializable]
public readonly record struct XenobiologyScanData(
    string TargetName,
    EntProtoId? Prototype,
    XenobiologyDevelopmentScanData? Development,
    XenobiologyNutritionScanData? Nutrition,
    XenobiologyMutationScanEntry[] Mutations);

[Serializable, NetSerializable]
public readonly record struct XenobiologyDevelopmentScanData(bool IsJuvenile, float Progress);

[Serializable, NetSerializable]
public readonly record struct XenobiologyNutritionScanData(float Hunger);

[Serializable, NetSerializable]
public readonly record struct XenobiologyMutationScanEntry(EntProtoId Target, float Progress);
