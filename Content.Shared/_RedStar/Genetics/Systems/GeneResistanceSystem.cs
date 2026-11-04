using Content.Shared._RedStar.Genetics.Components;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage.Systems;
using Content.Shared.Inventory;
using Content.Shared.StatusEffectNew;
using Content.Shared.StatusEffectNew.Components;
using Robust.Shared.Prototypes;

namespace Content.Shared._RedStar.Genetics.Systems;

public sealed partial class GeneResistanceSystem : EntitySystem
{
    [SubscribeLocalEvent(after: [typeof(InventorySystem), typeof(StatusEffectsSystem)])]
    private void OnModifyDamage(Entity<GenomeComponent> ent, ref DamageModifyEvent args)
    {
        if (!args.Damage.AnyPositive() || !TryComp<StatusEffectContainerComponent>(ent, out var container))
            return;

        Dictionary<ProtoId<DamageTypePrototype>, float>? reductions = null;
        var minimumMultiplier = 0f;
        foreach (var uid in container.ActiveStatusEffects?.ContainedEntities ?? [])
        {
            if (!TryComp<GeneResistanceComponent>(uid, out var resistance) ||
                !TryComp<StatusEffectComponent>(uid, out var status) || !status.Applied || TerminatingOrDeleted(uid))
                continue;

            reductions ??= new();
            minimumMultiplier = MathF.Max(minimumMultiplier, resistance.MinimumMultiplier);
            foreach (var (type, reduction) in resistance.Reductions)
            {
                reductions[type] = reductions.GetValueOrDefault(type) + reduction;
            }
        }

        if (reductions is null || reductions.Count == 0)
            return;

        var damage = new DamageSpecifier(args.Damage);
        foreach (var (type, amount) in args.Damage.DamageDict)
        {
            if (amount > 0 && reductions.TryGetValue(type, out var reduction))
                damage.DamageDict[type] = amount * MathF.Max(minimumMultiplier, 1f - reduction);
        }
        args.Damage = damage;
    }
}
