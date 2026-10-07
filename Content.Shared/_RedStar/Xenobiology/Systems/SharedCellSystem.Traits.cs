using Content.Shared._RedStar.Xenobiology.Components.Container;

namespace Content.Shared._RedStar.Xenobiology.Systems;

public abstract partial class SharedCellSystem
{
    /// <summary>
    /// Applies each newly acquired trait once; injected mutations are permanent.
    /// </summary>
    public void ApplyCellTraits(EntityUid target, Cell cell)
    {
        var host = EnsureComp<CellTraitHostComponent>(target);
        foreach (var traitId in cell.Traits)
        {
            if (!_prototype.TryIndex(traitId, out var prototype) ||
                !host.AcquiredTraits.Add(traitId))
                continue;

            _entityEffects.ApplyEffects(target, prototype.Effects);
        }
    }
}
