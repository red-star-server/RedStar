using Robust.Shared.Configuration;

namespace Content.Server._RedStar.Genetics;

[CVarDefs]
public sealed class GeneticsCVars
{
    public static readonly CVarDef<float> IncubationSeconds =
        CVarDef.Create("genetics.incubation_seconds", 60f, CVar.SERVERONLY);
}
