using Content.Shared._RedStar.Emoting.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Shared.Chat.Prototypes;

public sealed partial class EmotePrototype
{
    /// <summary>
    /// Optional visual animation played when this emote is performed.
    /// </summary>
    [DataField]
    public ProtoId<EmoteAnimationPrototype>? Animation;
}
