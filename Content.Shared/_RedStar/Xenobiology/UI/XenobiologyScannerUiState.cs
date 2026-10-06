using Robust.Shared.Serialization;

namespace Content.Shared._RedStar.Xenobiology.UI;

[Serializable, NetSerializable]
public enum XenobiologyScannerUiKey : byte
{
    Key
}

[Serializable, NetSerializable]
public sealed class XenobiologyScannerScannedMessage(XenobiologyScanData data) : BoundUserInterfaceMessage
{
    public XenobiologyScanData Data { get; } = data;
}
