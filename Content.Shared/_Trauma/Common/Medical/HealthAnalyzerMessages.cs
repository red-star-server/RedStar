using Robust.Shared.Serialization;
using Robust.Shared.Prototypes;

using Content.Shared._Trauma.Common.Body;
using Content.Shared._Trauma.Common.Traumas;
using Content.Shared.Body;
using Content.Shared.FixedPoint;

namespace Content.Shared._Trauma.Common.Medical.HealthAnalyzer;

// Part selection message (from client to server)
[Serializable, NetSerializable]
public sealed class HealthAnalyzerPartMessage(ProtoId<OrganCategoryPrototype>? category) : BoundUserInterfaceMessage
{
    public readonly ProtoId<OrganCategoryPrototype>? Category = category;
}

[Serializable, NetSerializable]
public enum HealthAnalyzerMode : byte
{
    Body,
    Organs,
    Chemicals
}
