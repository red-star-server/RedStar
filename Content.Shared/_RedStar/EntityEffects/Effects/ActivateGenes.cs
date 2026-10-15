using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;

namespace Content.Shared._RedStar.EntityEffects.Effects;

/// <summary>
/// Activates genetic material already present in the target's genome.
/// </summary>
public sealed partial class ActivateGenes : EntityEffectBase<ActivateGenes>
{
    public override string EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys) =>
        Loc.GetString("entity-effect-guidebook-activate-genes", ("chance", Probability));
}
