using Robust.Shared.Configuration;

namespace Content.Shared.Light;

[CVarDefs]
public static class DarkAmbientCVars
{
    public static readonly CVarDef<bool> Enabled =
        CVarDef.Create("light.dark_ambient_enabled", true, CVar.CLIENTONLY | CVar.ARCHIVE);

    public static readonly CVarDef<int> Strength =
        CVarDef.Create("light.dark_ambient_strength", 70, CVar.CLIENTONLY | CVar.ARCHIVE);
}
