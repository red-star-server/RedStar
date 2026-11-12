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

    public override void Apply(EntityUid target, EntityUid? surgeon, EntityUid? usedItem, IEntityManager entityManager)
    {
        var body = entityManager.System<BodySystem>().GetBody(target) ?? target;
        if (entityManager.System<StatusEffectsSystem>().HasEffectComp<ForcedSleepingStatusEffectComponent>(body))
            return;

        entityManager.System<SharedChatSystem>().TryEmoteWithChat(body, Emote, ignoreActionBlocker: true);
    }
}
