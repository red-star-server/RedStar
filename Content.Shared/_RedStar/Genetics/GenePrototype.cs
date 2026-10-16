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

    [DataField]
    public int FragmentCount = 4;

    [DataField]
    public float MinimumStability = 0.5f;

    [DataField]
    public EntityEffect[] Effects = [];
}
