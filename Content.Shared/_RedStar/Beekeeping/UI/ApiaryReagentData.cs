using Content.Shared.Chemistry.Reagent;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._RedStar.Beekeeping.UI;

[Serializable, NetSerializable]
public sealed class ApiaryReagentData
{
    public ProtoId<ReagentPrototype> Reagent;
    public float Amount;
}
