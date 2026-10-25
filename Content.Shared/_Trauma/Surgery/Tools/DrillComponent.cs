using Robust.Shared.GameStates;

using Content.Shared._Trauma.Common.Surgery.Tools;

namespace Content.Shared._Trauma.Surgery.Tools;

[RegisterComponent, NetworkedComponent]
public sealed partial class DrillComponent : BaseSurgeryToolComponent
{
    public override string ToolName => Loc.GetString("trauma-surgery-tool-drill");
}
