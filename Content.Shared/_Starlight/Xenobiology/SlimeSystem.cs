using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Interaction;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;
using Robust.Shared.Serialization;

namespace Content.Shared._Starlight.Xenobiology;

/// <summary>
/// Handles the general behavior of slimes.
/// </summary>
public sealed partial class SlimeSystem : EntitySystem
{
    [Dependency] private SharedInteractionSystem _interaction = default!;
    [Dependency] private DamageableSystem _damageableSystem = default!;
    [Dependency] private SatiationSystem _satiationSystem = default!;
    [Dependency] private SharedAudioSystem _audioSystem = default!;

    /// <summary>
    /// Attempts to eat a target.
    /// </summary>
    /// <param name="slime">The slime entity.</param>
    /// <param name="target">The target entity ID.</param>
    /// <returns>Returns false if the slime was unable to eat the target. Returns true otherwise.</returns>
    public bool TryEat(Entity<SlimeComponent?> slime, EntityUid target)
    {
        if (!Resolve(slime, ref slime.Comp, false)) return false;

        if (!_interaction.InRangeUnobstructed(slime.Owner, target, range: 0.75f)) return false;
        if (!TryComp<DamageableComponent>(target, out _)) return false;

        if (!_damageableSystem.TryChangeDamage(target, slime.Comp.DamageOnEat, out var returnDamage, ignoreResistances: true)) return false;
        _audioSystem.PlayPredicted(new SoundPathSpecifier("/Audio/Effects/bite.ogg"), slime.Owner, null, AudioParams.Default.WithVariation(0.05F));

        var vector = (Transform(target).LocalPosition - Transform(slime.Owner).LocalPosition).Normalized();
        RaiseNetworkEvent(new SlimeBiteAnimationMessage()
        {
            Entity = GetNetEntity(slime.Owner, MetaData(slime.Owner)),
            Angle = Angle.FromWorldVec(vector),
        }, Filter.Pvs(slime.Owner, 0.5F));

        if (returnDamage.AnyPositive() && TryComp<SatiationComponent>(slime, out var satiation))
        {
            _satiationSystem.ModifyValue((slime.Owner, satiation), SatiationSystem.Hunger, slime.Comp.NutritionOnHit.Float());
        }

        return true;
    }
}

[Serializable, NetSerializable]
public sealed class SlimeBiteAnimationMessage : EntityEventArgs
{
    public NetEntity Entity;
    public Angle Angle;
}
