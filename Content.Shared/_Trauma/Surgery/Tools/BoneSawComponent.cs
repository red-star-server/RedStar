using Robust.Shared.GameStates;

using Content.Shared._Trauma.Common.Surgery.Tools;

namespace Content.Shared._Trauma.Surgery.Tools;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class BoneSawComponent : BaseSurgeryToolComponent
{
    public override string ToolName => Loc.GetString("trauma-surgery-tool-bonesaw");
    [AutoNetworkedField]
    public new float Speed
    {
        get
        {
            return base.Speed;
        }
        set
        {
            base.Speed = value;
        }
    }
}
