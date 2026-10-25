using Robust.Shared.Serialization;

using Content.Shared.DoAfter;

namespace Content.Shared._Trauma.Power;

[Serializable, NetSerializable]
public sealed partial class BatteryDrinkerDoAfterEvent : SimpleDoAfterEvent
{
    public readonly NetEntity Battery;

    public BatteryDrinkerDoAfterEvent(NetEntity battery)
    {
        Battery = battery;
    }
}
