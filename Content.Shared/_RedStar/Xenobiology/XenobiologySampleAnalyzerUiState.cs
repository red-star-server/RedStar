using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._RedStar.Xenobiology;

[Serializable, NetSerializable]
public enum XenobiologySampleAnalyzerUiKey : byte
{
    Key
}

[Serializable, NetSerializable]
public enum XenobiologySampleStatus : byte
{
    Empty,
    NoServer,
    Unpowered,
    Unmatched,
    Ready,
    Analyzing
}

[Serializable, NetSerializable]
public readonly record struct XenobiologyResearchEntry(EntProtoId<SlimeExtractComponent> Sample, int Reward);

[Serializable, NetSerializable]
public sealed class XenobiologySampleAnalyzerUiState(
    XenobiologyResearchEntry[] targets,
    string? serverName,
    NetEntity? sample,
    EntProtoId<SlimeExtractComponent>? samplePrototype,
    XenobiologySampleStatus status,
    TimeSpan? analysisStart,
    TimeSpan? analysisEnd) : BoundUserInterfaceState
{
    public XenobiologyResearchEntry[] Targets { get; } = targets;
    public string? ServerName { get; } = serverName;
    public NetEntity? Sample { get; } = sample;
    public EntProtoId<SlimeExtractComponent>? SamplePrototype { get; } = samplePrototype;
    public XenobiologySampleStatus Status { get; } = status;
    public TimeSpan? AnalysisStart { get; } = analysisStart;
    public TimeSpan? AnalysisEnd { get; } = analysisEnd;
}

[Serializable, NetSerializable]
public sealed class XenobiologyAnalyzeSampleMessage : BoundUserInterfaceMessage;
