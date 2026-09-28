using Content.Shared.FixedPoint;

namespace Content.Shared._RedStar.Xenobiology;

public sealed partial class SlimeMutationSystem : EntitySystem
{
    public FixedPoint2 ClampMutationChance(Entity<SlimeLifecycleComponent> slime, FixedPoint2 chance)
        => FixedPoint2.Clamp(chance,
            FixedPoint2.New(slime.Comp.MinimumMutationChance),
            FixedPoint2.New(slime.Comp.MaximumMutationChance));

    public bool SetMutationChance(Entity<SlimeLifecycleComponent> slime, FixedPoint2 chance)
        => SetMutationChanceClamped(slime, ClampMutationChance(slime, chance));

    /// <summary>
    /// Stores a chance already clamped against its source lifecycle bounds, such as inherited offspring chance.
    /// </summary>
    public bool SetMutationChanceClamped(Entity<SlimeLifecycleComponent> slime, FixedPoint2 chance)
    {
        if (chance == slime.Comp.MutationChance)
            return false;

        slime.Comp.MutationChance = chance;
        return true;
    }

    public bool ModifyMutationChance(Entity<SlimeLifecycleComponent> slime, FixedPoint2 amount)
        => SetMutationChance(slime, slime.Comp.MutationChance + amount);
}
