using Content.Shared.Body;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

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
