using Content.Shared._Trauma.Body;
using Content.Shared._Trauma.Common.Body;
using Content.Shared.Body;
using Robust.Shared.Prototypes;

namespace Content.Shared._Trauma.Surgery.Graph.Conditions;

[Serializable]
public sealed partial class OrganOnAddCondition : SurgeryStepCondition
{
    [DataField(required: true)]
    public Dictionary<ProtoId<OrganCategoryPrototype>, ComponentRegistry> Components = new();

    [DataField]
    public bool Inverse;

    public override bool Check(EntityUid target, ProtoId<OrganCategoryPrototype>? organ, IEntityManager entityManager)
    {
        var organs = entityManager.System<BodyPartSystem>().GetPartOrgans(target);

        var allFound = true;
        var zeroFound = true;
        foreach (var (category, components) in Components)
        {
            if (!organs.TryGetValue(category, out var organUid)
                || !entityManager.TryGetComponent<OrganComponentsComponent>(organUid, out var comps))
            {
                allFound = false;
                continue;
            }

            foreach (var key in components.Keys)
            {
                if (!comps.AddedKeys.Contains(key))
                    allFound = false;
                else
                    zeroFound = false;
            }
        }

        return Inverse ? !allFound : !zeroFound;
    }
}