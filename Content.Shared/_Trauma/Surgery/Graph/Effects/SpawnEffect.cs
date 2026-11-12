using Content.Shared.Body;
using Robust.Shared.Prototypes;

namespace Content.Shared._Trauma.Surgery.Graph.Effects;

[Serializable]
public sealed partial class SpawnEffect : SurgeryStepEffect
{
    [DataField(required: true)]
    public EntProtoId Entity;

    public override void Apply(EntityUid target, EntityUid? surgeon, EntityUid? usedItem, IEntityManager entityManager)
    {
        var body = entityManager.System<BodySystem>().GetBody(target) ?? target;
        var coords = entityManager.GetComponent<TransformComponent>(body).Coordinates;
        ((EntityManager)entityManager).PredictedSpawnAtPosition(Entity, coords);
    }
}
