using Robust.Shared.GameStates;

namespace Content.Shared.Body.Components;

public sealed partial class BrainComponent
{
    [DataField, AutoNetworkedField]
    public bool Active = true;
}
