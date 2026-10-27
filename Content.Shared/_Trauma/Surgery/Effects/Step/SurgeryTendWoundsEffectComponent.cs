using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Trauma.Surgery.Effects.Step;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SurgeryTendWoundsEffectComponent : Component
{
    /// <summary>
    /// Primary damage group treated by this tending step.
    /// </summary>
    [DataField, AutoNetworkedField]
    public ProtoId<DamageGroupPrototype> MainGroup = "Brute";

    /// <summary>
    /// Whether tending repeats while treatable damage remains.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool IsAutoRepeatable = true;

    /// <summary>
    /// Base damage change applied by the tending step.
    /// </summary>
    [DataField, AutoNetworkedField]
    public DamageSpecifier Damage = default!;

    /// <summary>
    /// Multiplier used to scale healing with the patient's wounds.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float HealMultiplier = 0.07f;
}
