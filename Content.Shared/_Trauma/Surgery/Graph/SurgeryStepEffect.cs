namespace Content.Shared._Trauma.Surgery.Graph;

[ImplicitDataDefinitionForInheritors]
[Serializable]
public abstract partial class SurgeryStepEffect
{
    /// <summary>
    /// Applies the effect. Returns false when a mandatory action failed, which aborts the step without advancing the graph.
    /// </summary>
    public abstract bool Apply(EntityUid target, EntityUid? surgeon, EntityUid? usedItem, IEntityManager entityManager);
}
