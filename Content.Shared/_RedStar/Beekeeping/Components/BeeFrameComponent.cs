using Content.Shared.Chemistry.Components;
using Content.Shared.Guidebook;
using Content.Shared.Stacks;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RedStar.Beekeeping.Components;

/// <summary>
/// A hive frame. Bees fill it with combs while it sits in a hive; a sharp tool cuts the combs off.
/// The sprite (foundation / filling / capped) follows from <see cref="Combs"/> and <see cref="MaxCombs"/> through
/// the appearance data, so the same entity keeps all its data as the fill changes.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class BeeFrameComponent : Component
{
    /// <summary>
    /// Combs on the frame now.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int Combs;

    /// <summary>
    /// Combs the frame holds when full. A full frame is capped and gives wax when cut.
    /// </summary>
    [DataField, AutoNetworkedField, GuidebookData]
    public int MaxCombs = 1;

    /// <summary>
    /// Each piece of wax used on the frame adds one comb, up to this many.
    /// </summary>
    [DataField, GuidebookData]
    public int MaxCombsLimit = 30;

    [DataField]
    public ProtoId<StackPrototype> WaxStack = "Beeswax";

    /// <summary>
    /// What one comb on the frame becomes when cut.
    /// </summary>
    [DataField]
    public EntProtoId CombPrototype = "FoodHoneycomb";

    /// <summary>
    /// What cutting a capped frame gives besides the combs.
    /// </summary>
    [DataField]
    public EntProtoId WaxPrototype = "Beeswax";

    /// <summary>
    /// Wax from cutting the caps off a full frame. A frame that is not full gives none.
    /// </summary>
    [DataField]
    public int WaxPerCappedFrame = 1;

    /// <summary>
    /// Solution of a comb that the payload goes into when the frame is cut.
    /// </summary>
    [DataField]
    public string CombSolution = "food";

    /// <summary>
    /// Payload the combs on this frame took from the queen. Server only.
    /// </summary>
    [ViewVariables]
    public Solution Payload = new();

    /// <summary>
    /// What an untouched comb looks like; the reagents the queen added mix their colors into it.
    /// </summary>
    [DataField]
    public Color HoneyColor = Color.FromHex("#EDC200");

    /// <summary>
    /// Honey in one comb, to weigh it against the payload when the color is mixed.
    /// </summary>
    [DataField]
    public float HoneyPerComb = 5f;

    /// <summary>
    /// Seconds the cut takes.
    /// </summary>
    [DataField]
    public float CutDelay = 1f;

    /// <summary>
    /// A full frame whose wax caps were cut off: it shows as a honey frame until the combs are cut too.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Uncapped;
}
