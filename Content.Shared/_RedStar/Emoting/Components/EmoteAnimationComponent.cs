using Robust.Shared.GameStates;

namespace Content.Shared._RedStar.Emoting.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(raiseAfterAutoHandleState: true)]
public sealed partial class EmoteAnimationComponent : Component
{
    [DataField, AutoNetworkedField]
    public EmoteAnimationType Animation;

    [DataField, AutoNetworkedField]
    public uint AnimationSequence;

    [DataField]
    public Dictionary<string, EmoteAnimationType> Animations = new();
}
