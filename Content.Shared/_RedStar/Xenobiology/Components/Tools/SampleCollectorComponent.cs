using Content.Shared.Damage;

namespace Content.Shared._RedStar.Xenobiology.Components.Tools;

[RegisterComponent]
public sealed partial class SampleCollectorComponent : Component
{
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan Delay = TimeSpan.FromSeconds(4f);

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public DamageSpecifier? Damage;
}
