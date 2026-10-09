using Robust.Shared.Configuration;

namespace Content.Shared.Light;

[CVarDefs]
public static class LightAtmosphereCVars
{
    public static readonly CVarDef<bool> Enabled =
        CVarDef.Create("light.atmosphere_enabled", true, CVar.CLIENTONLY | CVar.ARCHIVE);
}
