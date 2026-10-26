using Content.Shared._Trauma.Common.Surgery.Tools;
using Robust.Shared.GameStates;

namespace Content.Shared._Trauma.Surgery.Tools;

[RegisterComponent, NetworkedComponent]
public sealed partial class BoneSetterComponent : BaseSurgeryToolComponent
{
    public override string ToolName => Loc.GetString("trauma-surgery-tool-bonesetter");
}
