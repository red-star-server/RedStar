using Content.Shared.EntityEffects;
using Content.Shared._RedStar.Genetics;
using Robust.Shared.Prototypes;

namespace Content.Server._RedStar.Genetics;

public sealed partial class GenomeExpressionSystem : EntitySystem
{
    [Dependency] private IPrototypeManager _prototypes = default!;
    [Dependency] private SharedEntityEffectsSystem _effects = default!;

    /// <summary>
    /// Applies the gene's effects and returns true if any effect passes its conditions,
    /// or if the gene has no effects. This does not guarantee that every effect was applied.
    /// </summary>
    public bool TryExpress(EntityUid uid, ProtoId<GenePrototype> id)
    {
        var effects = _prototypes.Index(id).Effects;
        if (effects.Length == 0 || _effects.TryApplyEffects(uid, effects))
            return true;

        Log.Warning($"Could not express gene {id} on {ToPrettyString(uid)}.");
        return false;
    }
}
