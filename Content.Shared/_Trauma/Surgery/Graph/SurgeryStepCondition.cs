using Content.Shared.Body;
using Robust.Shared.Prototypes;

namespace Content.Shared._Trauma.Surgery.Graph;

[ImplicitDataDefinitionForInheritors]
[Serializable]
public abstract partial class SurgeryStepCondition
{
    [DataField]
    public bool Inverted;

    public bool CheckWithInvert(EntityUid target, ProtoId<OrganCategoryPrototype>? organ, IEntityManager entityManager)
    {
        var result = Check(target, organ, entityManager);
        return Inverted ? !result : result;
    }

    public abstract bool Check(EntityUid target, ProtoId<OrganCategoryPrototype>? organ, IEntityManager entityManager);
}
