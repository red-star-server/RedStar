using Content.Shared._RedStar.Xenobiology.Components;
using Robust.Shared.Prototypes;

namespace Content.Server._RedStar.Xenobiology.Components;

/// <summary>
/// Xenobiology discoveries owned by a regular research server.
/// </summary>
[RegisterComponent]
public sealed partial class XenobiologyResearchDatabaseComponent : Component
{
    [DataField]
    public HashSet<EntProtoId<XenobiologySampleComponent>> DiscoveredSamples = [];
}
