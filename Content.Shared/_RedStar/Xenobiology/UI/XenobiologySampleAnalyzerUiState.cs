using Content.Shared._RedStar.Xenobiology.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._RedStar.Xenobiology.UI;

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
    Ready
}

[Serializable, NetSerializable]
public readonly record struct XenobiologyResearchEntry(EntProtoId<SlimeExtractComponent> Sample, int Reward);

[Serializable, NetSerializable]
public readonly record struct XenobiologyAnalysisResult(EntProtoId<SlimeExtractComponent> Sample, int Reward);

[Serializable, NetSerializable]
public sealed class XenobiologySampleAnalyzerUiState(
    XenobiologyResearchEntry[] targets,
    XenobiologyResearchEntry[] completedTargets,
    int remainingSamples,
    string? serverName,
    NetEntity? sample,
    EntProtoId<SlimeExtractComponent>? samplePrototype,
    XenobiologySampleStatus status,
    XenobiologyAnalysisResult? result) : BoundUserInterfaceState
{
    public XenobiologyResearchEntry[] Targets { get; } = targets;
    public XenobiologyResearchEntry[] CompletedTargets { get; } = completedTargets;
    public int RemainingSamples { get; } = remainingSamples;
    public string? ServerName { get; } = serverName;
    public NetEntity? Sample { get; } = sample;
    public EntProtoId<SlimeExtractComponent>? SamplePrototype { get; } = samplePrototype;
    public XenobiologySampleStatus Status { get; } = status;
    public XenobiologyAnalysisResult? Result { get; } = result;
}

[Serializable, NetSerializable]
public sealed class XenobiologyAnalyzeSampleMessage : BoundUserInterfaceMessage;
