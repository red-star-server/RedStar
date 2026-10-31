using Robust.Shared.Configuration;

namespace Content.Shared._Trauma.Common.CCVar;

[CVarDefs]
public static partial class TraumaCVars
{
    public static readonly CVarDef<bool> MouseWheelTargeting =
        CVarDef.Create("trauma.mouse_wheel_targeting", true, CVar.ARCHIVE | CVar.CLIENTONLY);
}
