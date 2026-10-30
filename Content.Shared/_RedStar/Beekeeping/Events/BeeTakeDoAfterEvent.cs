using Content.Shared.DoAfter;
using Content.Shared._RedStar.Beekeeping.Data;
using Robust.Shared.Serialization;

namespace Content.Shared._RedStar.Beekeeping.Events;

/// <summary>
/// Taking a bee out of a hive in the chosen role. Raised on the hive.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class BeeTakeDoAfterEvent : DoAfterEvent
{
    [DataField]
    public BeeRole Role;

    private BeeTakeDoAfterEvent()
    {
    }

    public BeeTakeDoAfterEvent(BeeRole role)
    {
        Role = role;
    }

    public override DoAfterEvent Clone() => this;
}
