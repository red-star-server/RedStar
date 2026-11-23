using Content.Shared.Chemistry.Reagent;
using Robust.Shared.Prototypes;

namespace Content.Shared._RedStar.Beekeeping.Prototypes;

/// <summary>
/// One kind of meat that can fall out of a broken meat hive. Each time the hive drops meat it picks one kind by weight.
/// The weight of a kind is <see cref="Weight"/> plus a bonus for every kind of blood in <see cref="BloodBonus"/> the
/// colony has brought, plus <see cref="VarietyBonus"/> for each kind of blood beyond the first, and it is zero while
/// the colony has fewer than <see cref="MinVariety"/> kinds. The meat does not have to come from blood the hive has.
/// </summary>
[Prototype]
public sealed partial class BeeMeatPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// What falls out when this kind is picked: one of each, so a mixed kind lists several.
    /// </summary>
    [DataField(required: true)]
    public List<EntProtoId> Entities = new();

    /// <summary>
    /// The weight the colony starts with, whatever it has bitten.
    /// </summary>
    [DataField]
    public float Weight = 1f;

    /// <summary>
    /// Added to the weight when the colony has brought this kind of blood.
    /// </summary>
    [DataField]
    public Dictionary<ProtoId<ReagentPrototype>, float> BloodBonus = new();

    /// <summary>
    /// Added for every kind of blood the colony has brought beyond the first.
    /// </summary>
    [DataField]
    public float VarietyBonus;

    /// <summary>
    /// Kinds of blood the colony needs before this kind can fall out at all.
    /// </summary>
    [DataField]
    public int MinVariety = 1;

    /// <summary>
    /// The weight of this kind for a colony that has brought the given kinds of blood.
    /// </summary>
    public float WeightFor(IReadOnlyList<ProtoId<ReagentPrototype>> bloods)
    {
        if (bloods.Count < MinVariety)
            return 0f;

        var weight = Weight + VarietyBonus * Math.Max(bloods.Count - 1, 0);
        foreach (var (blood, bonus) in BloodBonus)
        {
            if (System.Linq.Enumerable.Contains(bloods, blood))
                weight += bonus;
        }

        return weight;
    }
}
