using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared._RedStar.Xenobiology;

[RegisterComponent]
public sealed partial class SlimeScannerComponent : Component;

[Serializable, NetSerializable]
public enum SlimeScannerUiKey : byte
{
    Key
}

[Serializable, NetSerializable]
public sealed partial class SlimeScannerDoAfterEvent : SimpleDoAfterEvent;

[Serializable, NetSerializable]
public sealed class SlimeScannerScannedMessage(SlimeScanData data) : BoundUserInterfaceMessage
{
    public SlimeScanData Data { get; } = data;
}

[Serializable, NetSerializable]
public sealed class SlimeScannerSoundMessage : EntityEventArgs
{
    public NetEntity Owner;
    public NetEntity User;
}
