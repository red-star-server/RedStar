using Content.Shared._RedStar.Emoting.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RedStar.Emoting.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(raiseAfterAutoHandleState: true)]
public sealed partial class EmoteAnimationComponent : Component
{
    /// <summary>
    /// Animation most recently requested by the server.
    /// </summary>
    [AutoNetworkedField]
    public ProtoId<EmoteAnimationPrototype>? Animation;

    /// <summary>
    /// Incremented for every animation request so the same animation can be replayed consecutively.
    /// </summary>
    [AutoNetworkedField]
    public uint AnimationSequence;

    /// <summary>
    /// Last sequence handled by this client.
    /// </summary>
    [ViewVariables]
    public uint LastClientAnimationSequence;
}
