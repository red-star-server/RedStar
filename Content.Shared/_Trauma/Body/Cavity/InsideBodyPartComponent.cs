using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Content.Shared.Damage;
using Content.Shared.DoAfter;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._Trauma.Body.Cavity;

/// <summary>
/// Component added to items inserted to someone's chest cavity via surgery.
/// Mobs will be able to burst out with an action.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(InsideBodyPartSystem))]
[AutoGenerateComponentState]
public sealed partial class InsideBodyPartComponent : Component
{
    /// <summary>
    /// The part this entity is inside of.
    /// Should always be valid.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid Part;

    /// <summary>
    /// Action to grant for mobs (BodyComponent) inside of body parts.
    /// Not given to things like pAIs since they have no Body.
    /// It's assumed that anything with Body is able to claw its way out.
    /// </summary>
    [DataField]
    public EntProtoId<InstantActionComponent> BurstAction = "ActionBodyPartBurst";

    /// <summary>
    /// Spawned action that lets the contained entity burst out.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? ActionEntity;

    /// <summary>
    /// Time required to burst out of a severed part or a dead body.
    /// </summary>
    [DataField]
    public TimeSpan Delay = TimeSpan.FromSeconds(4);

    /// <summary>
    /// Time required to burst out while the containing body is alive.
    /// </summary>
    [DataField]
    public TimeSpan AliveDelay = TimeSpan.FromSeconds(10); // more drama when alive

    /// <summary>
    /// Damage dealt to the containing part when the entity escapes.
    /// </summary>
    [DataField]
    public DamageSpecifier BurstDamage = new()
    {
        DamageDict =
        {
            { "Blunt", 70 }
        }
    };

    /// <summary>
    /// How long to stun the victim for when bursting.
    /// </summary>
    [DataField]
    public TimeSpan StunTime = TimeSpan.FromSeconds(5);
}

/// <summary>
/// Action <c>BurstAction</c> has to use.
/// </summary>
public sealed partial class BodyPartBurstEvent : InstantActionEvent;

[Serializable, NetSerializable]
public sealed partial class BurstDoAfterEvent : SimpleDoAfterEvent;
