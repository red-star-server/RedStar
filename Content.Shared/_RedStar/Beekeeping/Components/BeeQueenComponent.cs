using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Content.Shared.Guidebook;
using Content.Shared._RedStar.Beekeeping.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RedStar.Beekeeping.Components;

/// <summary>
/// A queen bee as an item. Put into a hive, she keeps the colony growing. Her traits are the colony's traits.
/// Unstable mutagen injected into her rolls a random trait; with a sting, any other reagent becomes the payload her
/// bees carry, kept in her solution up to <see cref="PayloadCap"/>.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class BeeQueenComponent : Component
{
    /// <summary>
    /// The hive she was last taken out of. Left on the floor, she flies back into it if its queen slot is free.
    /// Server only.
    /// </summary>
    [ViewVariables]
    public EntityUid? LastHive;

    /// <summary>
    /// With no hive to fly back to, she looks for a new home this far away.
    /// </summary>
    [DataField]
    public float NestRange = 12f;

    /// <summary>
    /// What flies off with her when she leaves to found a hive.
    /// </summary>
    [DataField]
    public EntProtoId NestFlyerPrototype = "BeeQueenNesting";

    [DataField]
    public EntProtoId WildHivePrototype = "WildBeehive";

    /// <summary>
    /// How long she lives outside a hive: from unpacking or from being taken out. In a hive the clock stops.
    /// </summary>
    [DataField]
    public TimeSpan Lifetime = TimeSpan.FromMinutes(5);

    /// <summary>
    /// When she dies if she stays out of a hive. Null while she is in one.
    /// </summary>
    [DataField, AutoNetworkedField, AutoPausedField]
    public TimeSpan? DiesAt;

    /// <summary>
    /// The examine text warns this long before she dies.
    /// </summary>
    [DataField]
    public TimeSpan DyingWarning = TimeSpan.FromMinutes(1);

    /// <summary>
    /// Her traits, innate ones included.
    /// </summary>
    [DataField, AutoNetworkedField]
    public List<ProtoId<BeeTraitPrototype>> Traits = new();

    /// <summary>
    /// Traits she can hold besides the innate ones. A new trait with no free slot replaces a random one.
    /// </summary>
    [DataField, AutoNetworkedField, GuidebookData]
    public int Slots = 1;

    /// <summary>
    /// How many times mutagen has been rolled on her. The more, the likelier a roll gives a bad trait or takes a good
    /// one. A young queen raised from her egg starts again at zero.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int Instability;

    /// <summary>
    /// Her solution: what was injected and not used yet. Mutagen is used up at once, the rest is payload.
    /// </summary>
    [DataField]
    public string Solution = "queen";

    /// <summary>
    /// The reagent that rolls a trait.
    /// </summary>
    [DataField]
    public ProtoId<ReagentPrototype> Mutagen = "UnstableMutagen";

    /// <summary>
    /// Mutagen for one roll.
    /// </summary>
    [DataField, GuidebookData]
    public FixedPoint2 MutagenPerRoll = 5;

    /// <summary>
    /// Most payload she keeps; anything beyond it is lost.
    /// </summary>
    [DataField, GuidebookData]
    public FixedPoint2 PayloadCap = 15;

    /// <summary>
    /// Payload that goes into one comb as it is built.
    /// </summary>
    [DataField]
    public FixedPoint2 PayloadPerComb = 1;

    /// <summary>
    /// Mutagen injected but not enough for a roll yet. Server only.
    /// </summary>
    [ViewVariables]
    public FixedPoint2 MutagenProgress;

    /// <summary>
    /// The reagent that adds a trait slot.
    /// </summary>
    [DataField]
    public ProtoId<ReagentPrototype> Jelly = "RoyalJelly";

    /// <summary>
    /// Royal jelly for one more slot.
    /// </summary>
    [DataField, GuidebookData]
    public FixedPoint2 JellyPerSlot = 5;

    /// <summary>
    /// Most trait slots royal jelly can give.
    /// </summary>
    [DataField, GuidebookData]
    public int MaxSlots = 3;

    /// <summary>
    /// Royal jelly injected but not enough for a slot yet. Server only.
    /// </summary>
    [ViewVariables]
    public FixedPoint2 JellyProgress;
}
