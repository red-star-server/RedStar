namespace Content.Shared._Trauma.Surgery.Graph;

[ImplicitDataDefinitionForInheritors]
[Serializable]
public abstract partial class SurgeryStepCondition
{
    [DataField]
    public bool Inverted;

    public bool CheckWithInvert(EntityUid target, IEntityManager entityManager)
    {
        var result = Check(target, entityManager);
        return Inverted ? !result : result;
    }

    public abstract bool Check(EntityUid target, IEntityManager entityManager);
}
