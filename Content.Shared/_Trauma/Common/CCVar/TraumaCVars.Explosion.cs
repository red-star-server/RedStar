using Robust.Shared.Configuration;

namespace Content.Shared._Trauma.Common.CCVar;

public static partial class TraumaCVars
{
    public static readonly CVarDef<float> ExplosionLimbDamageVariation =
        CVarDef.Create("explosion.damage_variation", 2f, CVar.SERVERONLY);

    public static readonly CVarDef<float> ExplosionWoundMultiplier =
        CVarDef.Create("explosion.wounding_multiplier", 2f, CVar.SERVERONLY);
}
