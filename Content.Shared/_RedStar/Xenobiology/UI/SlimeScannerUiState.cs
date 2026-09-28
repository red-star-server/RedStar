using Robust.Shared.Serialization;

namespace Content.Shared._RedStar.Xenobiology.UI;

[Serializable, NetSerializable]
public enum SlimeScannerUiKey : byte
{
    Key
}

[Serializable, NetSerializable]
public sealed class SlimeScannerScannedMessage(SlimeScanData data) : BoundUserInterfaceMessage
{
    public SlimeScanData Data { get; } = data;
}
