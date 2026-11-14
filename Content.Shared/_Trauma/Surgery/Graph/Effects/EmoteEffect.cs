using Content.Shared.Bed.Sleep;
using Content.Shared.Body;
using Content.Shared.Chat;
using Content.Shared.Chat.Prototypes;
using Content.Shared.StatusEffectNew;
using Robust.Shared.Prototypes;

namespace Content.Shared._Trauma.Surgery.Graph.Effects;

[Serializable]
public sealed partial class EmoteEffect : SurgeryStepEffect
{
    [DataField]
    public ProtoId<EmotePrototype> Emote = "Scream";

    public override bool Apply(EntityUid target, EntityUid? surgeon, EntityUid? usedItem, ProtoId<OrganCategoryPrototype>? organ, IEntityManager entityManager)
    {
        var body = entityManager.System<BodySystem>().GetBody(target) ?? target;
        if (entityManager.System<StatusEffectsSystem>().HasEffectComp<ForcedSleepingStatusEffectComponent>(body))
            return true;

        entityManager.System<SharedChatSystem>().TryEmoteWithChat(body, Emote, ignoreActionBlocker: true);
        return true;
    }
}