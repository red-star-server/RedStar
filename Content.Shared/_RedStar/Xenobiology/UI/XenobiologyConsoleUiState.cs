using Content.Shared._RedStar.Xenobiology.Slimes;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._RedStar.Xenobiology.UI;

[Serializable, NetSerializable]
public enum XenobiologyConsoleUiKey : byte
{
    Key
}

[Serializable, NetSerializable]
public readonly record struct XenobiologyScannerEntry(NetEntity Entity, string DisplayName, bool IsDefaultName);

[Serializable, NetSerializable]
public readonly record struct XenobiologySlimeEntry(NetEntity Entity, string DisplayName, EntProtoId? Prototype, SlimeStage? Stage);

[Serializable, NetSerializable]
public sealed class XenobiologyConsoleUiState(
    XenobiologyScannerEntry[] linkedScanners,
    NetEntity? selectedScanner,
    XenobiologySlimeEntry[] detectedSlimes,
    NetEntity? selectedSlime,
    SlimeScanData? scan) : BoundUserInterfaceState
{
    public XenobiologyScannerEntry[] LinkedScanners { get; } = linkedScanners;
    public NetEntity? SelectedScanner { get; } = selectedScanner;
    public XenobiologySlimeEntry[] DetectedSlimes { get; } = detectedSlimes;
    public NetEntity? SelectedSlime { get; } = selectedSlime;
    public SlimeScanData? Scan { get; } = scan;
}

[Serializable, NetSerializable]
public sealed class XenobiologyConsoleSelectScannerMessage(NetEntity scanner) : BoundUserInterfaceMessage
{
    public NetEntity Scanner { get; } = scanner;
}

[Serializable, NetSerializable]
public sealed class XenobiologyConsoleSelectSlimeMessage(NetEntity slime) : BoundUserInterfaceMessage
{
    public NetEntity Slime { get; } = slime;
}
