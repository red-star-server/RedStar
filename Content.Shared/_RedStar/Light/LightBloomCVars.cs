using Robust.Shared.Configuration;

namespace Content.Shared.Light;

[CVarDefs]
public static class LightBloomCVars
{
    public static readonly CVarDef<bool> BloomEnabled =
        CVarDef.Create("light.bloom_enabled", true, CVar.CLIENTONLY | CVar.ARCHIVE);

    public static readonly CVarDef<float> BloomStrength =
        CVarDef.Create("light.bloom_strength", 0.7f, CVar.CLIENTONLY | CVar.ARCHIVE);

    public static readonly CVarDef<bool> BloomCones =
        CVarDef.Create("light.bloom_cones", true, CVar.CLIENTONLY | CVar.ARCHIVE);
}
