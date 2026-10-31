using Content.Shared.Body.Components;
using Content.Shared.Whitelist;

namespace Content.Shared.Body.Systems;

public sealed partial class StomachSystem
{
    /// <summary>
    /// Sets the stomach's special-food whitelist and whether it also accepts ordinary food.
    /// Callers that provide temporary diets must retain and restore the original settings.
    /// These fields are not networked by the stomach component; callers must synchronize their overrides.
    /// </summary>
    public void SetDiet(Entity<StomachComponent?> ent, EntityWhitelist? whitelist, bool exclusive)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return;

        ent.Comp.SpecialDigestible = whitelist;
        ent.Comp.IsSpecialDigestibleExclusive = exclusive;
    }
}
