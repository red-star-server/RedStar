using Content.Server._RedStar.Xenobiology.Slimes.Components;
using Content.Shared.FixedPoint;

namespace Content.Server._RedStar.Xenobiology.Slimes.Systems;

public sealed partial class SlimeMutationSystem : EntitySystem
{
    public FixedPoint2 ClampMutationChance(Entity<SlimeLifecycleComponent> slime, FixedPoint2 chance)
        => FixedPoint2.Clamp(chance,
            FixedPoint2.New(slime.Comp.MinimumMutationChance),
            FixedPoint2.New(slime.Comp.MaximumMutationChance));

    public bool SetMutationChance(Entity<SlimeLifecycleComponent> slime, FixedPoint2 chance)
        => SetMutationChanceUnchecked(slime, ClampMutationChance(slime, chance));

    /// <summary>
    /// Stores a chance without clamping it against the destination lifecycle bounds.
    /// The caller must clamp it against the appropriate source bounds, such as the parent's bounds for offspring.
    /// </summary>
    public bool SetMutationChanceUnchecked(Entity<SlimeLifecycleComponent> slime, FixedPoint2 chance)
    {
        if (chance == slime.Comp.MutationChance)
            return false;

        slime.Comp.MutationChance = chance;
        return true;
    }

    public bool ModifyMutationChance(Entity<SlimeLifecycleComponent> slime, FixedPoint2 amount)
        => SetMutationChance(slime, slime.Comp.MutationChance + amount);
}
