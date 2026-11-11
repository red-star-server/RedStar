using Content.Shared.Body;
using Content.Shared.Whitelist;
using Robust.Shared.Serialization;

namespace Content.Shared._Trauma.Surgery.Graph.Conditions;

[Serializable, NetSerializable]
public sealed partial class BodyComponentCondition : SurgeryStepCondition
{
    [DataField]
    public EntityWhitelist? Whitelist;

    [DataField]
    public EntityWhitelist? Blacklist;

    public override bool Check(EntityUid target, IEntityManager entityManager)
    {
        var body = entityManager.System<BodySystem>().GetBody(target) ?? target;
        return entityManager.System<EntityWhitelistSystem>().CheckBoth(body, Blacklist, Whitelist);
    }
}
