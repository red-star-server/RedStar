using Content.Shared.Chemistry.Components;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Content.Shared.Guidebook;
using Content.Shared._RedStar.Beekeeping.Data;
using Content.Shared._RedStar.Beekeeping.Prototypes;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RedStar.Beekeeping.Components;

/// <summary>
/// A bee of a hive that can be held: the same creature flies errands and sits in a hand. The role never changes: a
/// scout remembers trays for its hive, a pollinator pollinates plants. The bee lives for its <c>TimedDespawn</c>
/// lifetime unless it is brought back to its own hive.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class BeeItemComponent : Component
{
    [DataField]
    public BeeRole Role = BeeRole.Pollinator;

    /// <summary>
    /// The quiet buzz while it is held or packed away; free, it buzzes with the ambient sound of its prototype.
    /// </summary>
    [DataField]
    public SoundSpecifier HeldSound = new SoundPathSpecifier("/Audio/_RedStar/Ambience/Beekeeping/bee_in_hand.ogg");

    /// <summary>
    /// The ambient sound of the prototype, kept while the held buzz plays.
    /// </summary>
    [ViewVariables]
    public SoundSpecifier? FreeSound;

    /// <summary>
    /// The colony's traits, copied from the queen when the bee is taken out.
    /// </summary>
    [DataField, AutoNetworkedField]
    public List<ProtoId<BeeTraitPrototype>> Traits = new();

    #region Bite

    /// <summary>
    /// An aggressive bee without a sting bites a bare hand that picks it up. Any gloves stop it.
    /// </summary>
    [DataField]
    public DamageSpecifier? BiteDamage = new() { DamageDict = { ["Piercing"] = 1 } };

    /// <summary>
    /// An aggressive bee with a sting stings a hand that picks it up, gloves or not, unless the gloves are
    /// <see cref="StingProofGloves"/>. The sting also injects some of <see cref="Payload"/>.
    /// </summary>
    [DataField]
    public DamageSpecifier? StingDamage = new() { DamageDict = { ["Piercing"] = 1, ["Poison"] = 2 } };

    [DataField]
    public List<EntProtoId> StingProofGloves = new() { "ClothingHandsGlovesLeather" };

    /// <summary>
    /// What its sting carries, taken from the queen when the bee is taken out. Server only.
    /// </summary>
    [ViewVariables]
    public Solution Payload = new();

    [DataField]
    public FixedPoint2 PayloadPerSting = 1;

    #endregion

    #region Pollinator

    /// <summary>
    /// Pollinations left.
    /// </summary>
    [DataField, AutoNetworkedField, GuidebookData]
    public int Charges = 3;

    /// <summary>
    /// Chance that a pollination doubles the next harvest. The roll happens when pollinating.
    /// </summary>
    [DataField]
    public float DoubleYieldChance = 0.25f;

    /// <summary>
    /// Harvest multiplier set while a successful pollination lasts.
    /// </summary>
    [DataField]
    public int BonusYieldMod = 2;

    /// <summary>
    /// How long a pollination lasts if the plant is not harvested.
    /// </summary>
    [DataField]
    public TimeSpan PollinationDuration = TimeSpan.FromMinutes(20);

    /// <summary>
    /// Mutation level a pollination adds to the plant, from the colony's traits.
    /// </summary>
    [DataField]
    public float PollinationMutation;

    /// <summary>
    /// Whether pollination also fights pests and weeds in the tray.
    /// </summary>
    [DataField]
    public bool PestControl = true;

    /// <summary>
    /// Pest level removed by one pollination with <see cref="PestControl"/>.
    /// </summary>
    [DataField]
    public float PestReduction = 1f;

    /// <summary>
    /// Weed level removed by one pollination with <see cref="PestControl"/>.
    /// </summary>
    [DataField]
    public float WeedReduction = 2f;

    #endregion

    #region Scout

    /// <summary>
    /// Trays a scout can remember. A new one beyond that replaces the oldest.
    /// </summary>
    [DataField, GuidebookData]
    public int MemoryLimit = 3;

    /// <summary>
    /// How many trays the scout remembers, for the examine text.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int MemoryCount;

    /// <summary>
    /// Remembered trays, oldest first. Server only.
    /// </summary>
    [ViewVariables]
    public List<EntityUid> Memory = new();

    #endregion

    /// <summary>
    /// Seconds applying the bee to a tray takes.
    /// </summary>
    [DataField]
    public float ApplyDelay = 2f;

    /// <summary>
    /// When the bee dies, for the examine warning.
    /// </summary>
    [DataField, AutoNetworkedField, AutoPausedField]
    public TimeSpan ExpiresAt;

    /// <summary>
    /// The examine text warns this long before the bee dies.
    /// </summary>
    [DataField]
    public TimeSpan DyingWarning = TimeSpan.FromMinutes(1);

    /// <summary>
    /// The hive the bee came from and the only one it can return to.
    /// </summary>
    [ViewVariables]
    public EntityUid? SourceHive;

    /// <summary>
    /// Whether this bee is still counted in its hive's <c>OutBees</c>. Every way of releasing the reserve
    /// clears it first, so the reserve is released exactly once.
    /// </summary>
    [ViewVariables]
    public bool CountedOutside;

    /// <summary>
    /// Set when the lifetime ran out, as opposed to the bee being killed or destroyed earlier.
    /// </summary>
    [ViewVariables]
    public bool Expired;

    /// <summary>
    /// It pollinated or remembered a tray. Back home it needs a rest before it can be taken out again.
    /// </summary>
    [ViewVariables]
    public bool Worked;
}
