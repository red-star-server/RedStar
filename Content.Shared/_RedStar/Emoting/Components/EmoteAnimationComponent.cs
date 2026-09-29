using Content.Shared.Chat.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RedStar.Emoting.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(raiseAfterAutoHandleState: true)]
public sealed partial class EmoteAnimationComponent : Component
{
    /// <summary>
    /// Maps emote prototypes to their visual animations.
    /// </summary>
    [DataField]
    public Dictionary<ProtoId<EmotePrototype>, EmoteAnimationType> Animations = new();

    /// <summary>
    /// Animation currently requested by the server.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EmoteAnimationType Animation;

    /// <summary>
    /// Incremented whenever an animation is requested.
    /// Allows the same animation to be played repeatedly.
    /// </summary>
    [DataField, AutoNetworkedField]
    public uint AnimationSequence;

    /// <summary>
    /// Last sequence processed by this client.
    /// </summary>
    [ViewVariables]
    public uint LastClientAnimationSequence;
}
