using Content.Shared._RedStar.Xenobiology.Components;
using Robust.Shared.Prototypes;

namespace Content.Shared._RedStar.Xenobiology.Prototypes;

[Prototype]
public sealed partial class XenobiologyResearchPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public EntProtoId<XenobiologySampleComponent> Sample { get; private set; }

    [DataField(required: true)]
    public int Reward { get; private set; }
}
