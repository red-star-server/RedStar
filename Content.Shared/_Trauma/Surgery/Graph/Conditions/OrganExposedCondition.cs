using Content.Shared.Body;
using Robust.Shared.Prototypes;

namespace Content.Shared._Trauma.Surgery.Graph.Conditions;

[Serializable]
public sealed partial class OrganExposedCondition : SurgeryStepCondition
{
    public override bool Check(EntityUid target, ProtoId<OrganCategoryPrototype>? organ, IEntityManager entityManager)
    {
        return organ is { } category &&
               entityManager.System<SurgeryGraphSystem>().IsOrganExposed(target, category);
    }
}
