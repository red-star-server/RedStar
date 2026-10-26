using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

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
