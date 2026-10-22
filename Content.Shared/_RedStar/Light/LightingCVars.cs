using Robust.Shared.Configuration;

namespace Content.Shared.Light;

[CVarDefs]
public static class LightingCVars
{
    public static readonly CVarDef<bool> BloomEnabled =
        CVarDef.Create("light.bloom_enabled", true, CVar.CLIENTONLY | CVar.ARCHIVE);

    public static readonly CVarDef<int> BloomStrength =
        CVarDef.Create("light.bloom_strength", 100, CVar.CLIENTONLY | CVar.ARCHIVE);
}
