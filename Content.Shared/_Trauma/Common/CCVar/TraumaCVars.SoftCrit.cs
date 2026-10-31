using Robust.Shared.Configuration;

namespace Content.Shared._Trauma.Common.CCVar;

public static partial class TraumaCVars
{
    public static readonly CVarDef<float> BleedMultiplier =
        CVarDef.Create("medical.bloodloss_multiplier", 4.0f, CVar.SERVER | CVar.REPLICATED);

    public static readonly CVarDef<float> SoftCritMoveSpeed =
        CVarDef.Create("trauma.softcrit_move_speed", 0.5f, CVar.SERVER | CVar.REPLICATED);

    public static readonly CVarDef<float> SoftCritInhaleModifier =
        CVarDef.Create("trauma.softcrit_inhale_modifier", 0.3f, CVar.SERVER | CVar.REPLICATED);
}
