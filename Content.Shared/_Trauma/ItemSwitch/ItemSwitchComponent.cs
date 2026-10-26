using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared._Trauma.ItemSwitch;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class ItemSwitchComponent : Component
{
    /// <summary>
    ///     The item's toggle state.
    /// </summary>
    [DataField, AutoNetworkedField]
    public string State;

    /// <summary>
    /// State definitions indexed by the identifiers used by this item.
    /// </summary>
    [DataField(readOnly: true)]
    public Dictionary<string, ItemSwitchState> States = [];

    /// <summary>
    /// Can the entity be activated in the world.
    /// </summary>
    [DataField]
    public bool OnActivate = true;

    /// <summary>
    /// If this is set to false then the item can't be toggled by pressing Z.
    /// Use another system to do it then.
    /// </summary>
    [DataField]
    public bool OnUse = true;

    /// <summary>
    ///     Whether the item's toggle can be predicted by the client.
    /// </summary>
    /// <remarks>
    /// If server-side systems affect the item's toggle, then the item is not predictable.
    /// </remarks>
    [DataField]
    public bool Predictable = true;

    /// <summary>
    ///     Whether the item's currently toggled state should be shown in the UI.
    /// </summary>
    [DataField]
    public bool ShowLabel;

    /// <summary>
    ///     Whether the item requires power to sustain a state.
    /// </summary>
    [DataField]
    public bool NeedsPower;

    /// <summary>
    ///     Whether the item currently has enough power to sustain a state.
    /// </summary>
    [DataField]
    public bool IsPowered = true;

    /// <summary>
    ///     The default state of an item, which is also the state it reverts to when out of power.
    /// </summary>
    [DataField]
    public string? DefaultState;

    public ItemSwitchComponent(string state)
    {
        State = state;
    }
}

[DataDefinition]
public sealed partial class ItemSwitchState
{
    /// <summary>
    /// Localization key for the verb that activates this state.
    /// </summary>
    [DataField]
    public string? Verb;

    /// <summary>
    /// Sound played when the item enters this state.
    /// </summary>
    [DataField]
    public SoundSpecifier? SoundStateActivate;

    /// <summary>
    /// Sound played when activation of this state fails.
    /// </summary>
    [DataField]
    public SoundSpecifier? SoundFailToActivate;

    /// <summary>
    /// Components applied while this state is selected.
    /// </summary>
    [DataField]
    public ComponentRegistry? Components;

    /// <summary>
    /// Whether to remove this state's components when leaving it.
    /// </summary>
    [DataField]
    public bool RemoveComponents = true;

    /// <summary>
    /// Optional sprite used to represent this state.
    /// </summary>
    [DataField]
    public SpriteSpecifier? Sprite;

    /// <summary>
    /// Whether this state is hidden from the selection UI.
    /// </summary>
    [DataField]
    public bool Hidden;

    /// <summary>
    ///     Amount of energy consumed per swing
    /// </summary>
    [DataField]
    public int EnergyPerUse;

    public ItemSwitchState(string verb)
    {
        Verb = verb;
    }
}

/// <summary>
/// Raised directed on an entity when its ItemToggle is attempted to be activated.
/// </summary>
[ByRefEvent]
public record struct ItemSwitchAttemptEvent(EntityUid? User, string State, bool Cancelled = false, string? Popup = null);

/// <summary>
/// Raised directed on an entity any sort of toggle is complete.
/// </summary>
[ByRefEvent]
public readonly record struct ItemSwitchedEvent(EntityUid? User, string State, bool Predicted);
