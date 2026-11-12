using Robust.Shared.Prototypes;

namespace Content.Shared._Trauma.Surgery.Graph.Conditions;

[Serializable]
public sealed partial class PartComponentCondition : SurgeryStepCondition
{
    [DataField(required: true)]
    public ComponentRegistry Components = new();

    public override bool Check(EntityUid target, IEntityManager entityManager)
    {
        foreach (var reg in Components.Values)
        {
            if (!entityManager.HasComponent(target, reg.Component.GetType()))
                return false;
        }

        return true;
    }
}
