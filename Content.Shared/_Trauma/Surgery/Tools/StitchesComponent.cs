using Content.Shared._Trauma.Common.Surgery.Tools;
using Robust.Shared.GameStates;

namespace Content.Shared._Trauma.Surgery.Tools;

/// <summary>
/// Allows the item to close surgical incisions with stitches.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class StitchesComponent : BaseSurgeryToolComponent
{
    public override string ToolName => Loc.GetString("trauma-surgery-tool-stitches");
}
