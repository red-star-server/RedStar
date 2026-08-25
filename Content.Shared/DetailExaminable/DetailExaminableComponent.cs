using Content.Shared._Sirena.Humanoid;
using Robust.Shared.GameStates;

namespace Content.Shared.DetailExaminable;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class DetailExaminableComponent : Component
{
    [DataField(required: true), AutoNetworkedField]
    public string Content = string.Empty;

    // RS14-start
    [DataField, AutoNetworkedField]
    public ErpStatus ErpStatus = ErpStatus.No;
    // RS14-end
}
