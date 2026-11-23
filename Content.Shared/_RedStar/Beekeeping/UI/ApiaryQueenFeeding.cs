using Content.Shared.Chemistry.Reagent;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._RedStar.Beekeeping.UI;

/// <summary>
/// What injecting a queen does: so many units of mutagen roll a trait, so many of royal jelly add a slot, and with a
/// sting whatever else goes in stays as payload. The progress is what she has taken in towards the next one.
/// </summary>
[Serializable, NetSerializable]
public sealed class ApiaryQueenFeeding
{
    public ProtoId<ReagentPrototype> Mutagen;
    public float MutagenProgress;
    public float MutagenPerRoll;
    public ProtoId<ReagentPrototype> Jelly;
    public float JellyProgress;
    public float JellyPerSlot;
    public int MaxSlots;
}
