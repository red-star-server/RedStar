using Content.Server._RedStar.Xenobiology.Components;
using Content.Server._RedStar.Xenobiology.Slimes.Components;
using Content.Shared.Mobs.Systems;

namespace Content.Server._RedStar.Xenobiology.Systems;

public sealed partial class XenobiologyCellScannerSystem : EntitySystem
{
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private SharedTransformSystem _transform = default!;

    [Dependency] private EntityQuery<SlimeLifecycleComponent> _slimeQuery;

    /// <summary>
    /// Runs a fresh spatial lookup, including for client selection validation.
    /// Only living, uncontained Xenobiology slimes are returned.
    /// </summary>
    public HashSet<EntityUid> DetectSlimes(Entity<XenobiologyCellScannerComponent> scanner)
    {
        var result = new HashSet<EntityUid>();
        if (scanner.Comp.DetectionRadius <= 0f)
            return result;

        // The untyped overload always uses the local broadphase. The component
        // overload can fall back to enumerating every entity with that component.
        _lookup.GetEntitiesInRange(scanner.Owner, scanner.Comp.DetectionRadius, result, LookupFlags.Uncontained);
        result.RemoveWhere(slime => TerminatingOrDeleted(slime) ||
                                    !_slimeQuery.HasComp(slime) ||
                                    !_mobState.IsAlive(slime) ||
                                    !_transform.InRange(scanner.Owner, slime, scanner.Comp.DetectionRadius));
        return result;
    }
}
