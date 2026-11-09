using Robust.Shared;
using Robust.Shared.Configuration;

namespace Content.Shared._Trauma.Common.CCVar;

[CVarDefs]
public sealed class SurgeryCVars : CVars
{
    /// <summary>
    /// Whether or not players can operate on themselves.
    /// </summary>
    public static readonly CVarDef<bool> CanOperateOnSelf =
        CVarDef.Create("surgery.can_operate_on_self", true, CVar.SERVER | CVar.REPLICATED);

    /// <summary>
    /// Whether using a surgery tool on a lying patient performs the next surgery step on the targeted body part
    /// instead of requiring the surgery menu.
    /// </summary>
    public static readonly CVarDef<bool> WorldTarget =
        CVarDef.Create("surgery.world_target", true, CVar.SERVER | CVar.REPLICATED);

    /// <summary>
    /// The rate at which severity (wound) points get exchanged into bleeding; e.g., 50 severity would be 3.5 bleeding points.
    /// </summary>
    public static readonly CVarDef<float> BleedingSeverityTrade =
        CVarDef.Create("bleeds.wound_severity_trade", 0.15f, CVar.SERVER | CVar.REPLICATED);

    /// <summary>
    /// How quick by default do bleeds grow to their full form?
    /// </summary>
    public static readonly CVarDef<float> BleedsScalingTime =
        CVarDef.Create("bleeds.bleeding_scaling_time", 60f, CVar.SERVER | CVar.REPLICATED);
}
