using Robust.Shared.GameStates;

using Content.Shared._Trauma.Common.Surgery.Tools;

namespace Content.Shared._Trauma.Surgery.Tools;

/// <summary>
///     It lets you fucking stitch your ass up
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class StitchesComponent : BaseSurgeryToolComponent
{
    public override string ToolName => Loc.GetString("trauma-surgery-tool-stitches");
}
