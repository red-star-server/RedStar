using Content.Shared.Body;
using Robust.Shared.Prototypes;

namespace Content.Shared._Trauma.Surgery.Graph;

[ImplicitDataDefinitionForInheritors]
[Serializable]
public abstract partial class SurgeryStepEffect
{
    /// <summary>
    /// Applies the effect. Returns false when a mandatory action failed, which aborts the step without advancing the stage.
    /// Organ is the category of the concrete procedure instance, null for non-parameterized procedures.
    /// </summary>
    public abstract bool Apply(EntityUid target, EntityUid? surgeon, EntityUid? usedItem, ProtoId<OrganCategoryPrototype>? organ, IEntityManager entityManager);
}