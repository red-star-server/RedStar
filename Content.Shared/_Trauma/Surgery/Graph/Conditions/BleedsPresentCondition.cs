using Content.Shared._Trauma.Wounds;
using Robust.Shared.Serialization;

namespace Content.Shared._Trauma.Surgery.Graph.Conditions;

[Serializable, NetSerializable]
public sealed partial class BleedsPresentCondition : SurgeryStepCondition
{
    public override bool Check(EntityUid target, IEntityManager entityManager)
    {
        return entityManager.TryGetComponent<WoundableComponent>(target, out var woundable) && woundable.Bleeds > 0;
    }
}
