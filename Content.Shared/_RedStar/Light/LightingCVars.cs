using Robust.Shared.Configuration;

namespace Content.Shared.Light;

[CVarDefs]
public static class LightingCVars
{
    public static readonly CVarDef<bool> DeepShadowsEnabled =
        CVarDef.Create("light.atmosphere_enabled", true, CVar.CLIENTONLY | CVar.ARCHIVE);

    public static readonly CVarDef<int> ShadowStrength =
        CVarDef.Create("light.darkening_strength", 75, CVar.CLIENTONLY | CVar.ARCHIVE);

    public static readonly CVarDef<bool> HighlightCompressionEnabled =
        CVarDef.Create("light.highlights_enabled", true, CVar.CLIENTONLY | CVar.ARCHIVE);

    public static readonly CVarDef<bool> BloomEnabled =
        CVarDef.Create("light.bloom_enabled", true, CVar.CLIENTONLY | CVar.ARCHIVE);
}
