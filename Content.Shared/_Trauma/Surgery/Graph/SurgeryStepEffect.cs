namespace Content.Shared._Trauma.Surgery.Graph;

[ImplicitDataDefinitionForInheritors]
[Serializable]
public abstract partial class SurgeryStepEffect
{
    public abstract void Apply(EntityUid target, EntityUid? surgeon, EntityUid? usedItem, IEntityManager entityManager);
}
