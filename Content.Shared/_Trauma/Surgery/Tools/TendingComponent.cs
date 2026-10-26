using Content.Shared._Trauma.Common.Surgery.Tools;
using Robust.Shared.GameStates;

namespace Content.Shared._Trauma.Surgery.Tools;

/// <summary>
///     Like Hemostat but lets ghetto tools be used differently for clamping and tending wounds.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class TendingComponent : BaseSurgeryToolComponent
{
    public override string ToolName => Loc.GetString("trauma-surgery-tool-tending");
}
