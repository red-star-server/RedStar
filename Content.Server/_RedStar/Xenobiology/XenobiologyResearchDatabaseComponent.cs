using Content.Shared._RedStar.Xenobiology;
using Robust.Shared.Prototypes;

namespace Content.Server._RedStar.Xenobiology;

/// <summary>
/// Xenobiology research progress owned by a regular research server.
/// </summary>
[RegisterComponent]
public sealed partial class XenobiologyResearchDatabaseComponent : Component
{
    [DataField]
    public int ActiveTargetCount = 3;

    [DataField]
    public HashSet<ProtoId<XenobiologyResearchPrototype>> CompletedTargets = [];

    [DataField]
    public List<ProtoId<XenobiologyResearchPrototype>> ActiveTargets = [];
}
