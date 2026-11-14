using Content.Shared._Trauma.Body;
using Content.Shared._Trauma.Common.Body;
using Content.Shared.Body;
using Robust.Shared.Prototypes;

namespace Content.Shared._Trauma.Surgery.Graph.Conditions;

[Serializable]
public sealed partial class PartTypeCondition : SurgeryStepCondition
{
    [DataField]
    public HashSet<BodyPartType> Parts = [];

    [DataField]
    public BodyPartSymmetry? Symmetry;

    public override bool Check(EntityUid target, ProtoId<OrganCategoryPrototype>? organ, IEntityManager entityManager)
    {
        return entityManager.TryGetComponent<BodyPartComponent>(target, out var part)
            && (Parts.Count == 0 || Parts.Contains(part.PartType))
            && (Symmetry == null || part.Symmetry == Symmetry);
    }
}