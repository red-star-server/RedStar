using Content.Shared.Body;
using Robust.Shared.Prototypes;

namespace Content.Shared._Trauma.Surgery.Graph;

[ImplicitDataDefinitionForInheritors]
[Serializable]
public abstract partial class SurgeryStepCondition
{
    [DataField]
    public bool Inverted;

    /// <summary>
    /// Organ category of the concrete procedure instance, null for non-parameterized procedures.
    /// Organ-aware conditions use it when their own organ field is unset.
    /// </summary>
    public bool CheckWithInvert(EntityUid target, ProtoId<OrganCategoryPrototype>? organ, IEntityManager entityManager)
    {
        var result = Check(target, organ, entityManager);
        return Inverted ? !result : result;
    }

    public abstract bool Check(EntityUid target, ProtoId<OrganCategoryPrototype>? organ, IEntityManager entityManager);
}