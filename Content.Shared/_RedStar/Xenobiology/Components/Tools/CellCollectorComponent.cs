using Content.Shared.Damage;
using Robust.Shared.GameStates;

namespace Content.Shared._RedStar.Xenobiology.Components.Tools;

[RegisterComponent, NetworkedComponent]
public sealed partial class CellCollectorComponent : Component
{
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan Delay = TimeSpan.FromSeconds(4f);

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public DamageSpecifier? Damage;
}
