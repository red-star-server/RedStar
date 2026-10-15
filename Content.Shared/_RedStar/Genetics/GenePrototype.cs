using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;

namespace Content.Shared._RedStar.Genetics;

[Prototype]
public sealed partial class GenePrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = string.Empty;

    [DataField]
    public int Tier = 1;

    public int FragmentCount => Tier + 3;
    public float MinimumStability => (40 + Tier * 10) / 100f;

    [DataField]
    public EntityEffect[] Effects = [];
}
