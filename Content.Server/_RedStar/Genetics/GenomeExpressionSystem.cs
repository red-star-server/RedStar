using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;

namespace Content.Server._RedStar.Genetics;

public sealed partial class GenomeExpressionSystem : EntitySystem
{
    [Dependency] private IPrototypeManager _prototypes = default!;
    [Dependency] private SharedEntityEffectsSystem _effects = default!;

    [SubscribeLocalEvent]
    private void OnChanged(Entity<GenomeComponent> ent, ref GenomeChangedEvent args)
    {
        foreach (var id in args.Activated)
        {
            var effects = _prototypes.Index(id).Effects;
            if (effects.Length > 0 && !_effects.TryApplyEffects(ent.Owner, effects))
                Log.Warning($"Could not express gene {id} on {ToPrettyString(ent)}.");
        }
    }
}
