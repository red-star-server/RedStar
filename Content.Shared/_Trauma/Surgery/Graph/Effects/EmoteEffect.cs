using Content.Shared.Body;
using Content.Shared.Chat;
using Content.Shared.Chat.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Shared._Trauma.Surgery.Graph.Effects;

[Serializable]
public sealed partial class EmoteEffect : SurgeryStepEffect
{
    [DataField]
    public ProtoId<EmotePrototype> Emote = "Scream";

    public override bool Apply(EntityUid target, EntityUid? surgeon, EntityUid? usedItem, ProtoId<OrganCategoryPrototype>? organ, IEntityManager entityManager)
    {
        var patient = GetPatient(target, entityManager);
        if (IsAsleep(patient, entityManager))
            return true;

        entityManager.System<SharedChatSystem>().TryEmoteWithChat(patient, Emote, ignoreActionBlocker: true);
        return true;
    }
}
