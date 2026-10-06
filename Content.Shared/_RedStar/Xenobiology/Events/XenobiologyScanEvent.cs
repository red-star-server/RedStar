using Content.Shared._RedStar.Xenobiology.UI;
using Robust.Shared.Prototypes;

namespace Content.Shared._RedStar.Xenobiology.Events;

public record struct XenobiologyScanEvent(EntityUid Target, string TargetName, EntProtoId? Prototype)
{
    public XenobiologyDevelopmentScanData? Development;
    public XenobiologyNutritionScanData? Nutrition;
    public List<XenobiologyMutationScanEntry> Mutations { get; } = [];

    public XenobiologyScanData Build() => new(TargetName, Prototype, Development, Nutrition, Mutations.ToArray());
}
