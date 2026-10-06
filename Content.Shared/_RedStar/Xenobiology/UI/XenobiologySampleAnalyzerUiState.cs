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
    Researched,
    Ready,
    Processing,
    Complete
}

[Serializable, NetSerializable]
public sealed class XenobiologySampleAnalyzerUiState(
    NetEntity? sample,
    XenobiologySampleStatus status,
    float progress,
    int? reward) : BoundUserInterfaceState
{
    public NetEntity? Sample { get; } = sample;
    public XenobiologySampleStatus Status { get; } = status;
    public float Progress { get; } = progress;
    public int? Reward { get; } = reward;
}

[Serializable, NetSerializable]
public sealed class XenobiologyAnalyzeSampleMessage : BoundUserInterfaceMessage;
