using Robust.Shared.Prototypes;
using Robust.Shared.GameStates;

using Content.Shared.Chat.Prototypes;

namespace Content.Shared._Trauma.Surgery.Effects.Step;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SurgeryStepEmoteEffectComponent : Component
{
    [DataField, AutoNetworkedField]
    public ProtoId<EmotePrototype> Emote = "Scream";
}
