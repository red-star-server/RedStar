using Content.Shared._Trauma.Body;
using Content.Shared._Trauma.Common.CCVar;
using Content.Shared.Body;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Explosion;
using Robust.Shared.Prototypes;

namespace Content.Server.Explosion.EntitySystems;

public sealed partial class ExplosionSystem
{
    [Dependency] private BodySystem _body = default!;
    [Dependency] private BodyPartSystem _bodyParts = default!;

    private float PartVariation;
    private float WoundMultiplier;

    private ProtoId<DamageTypePrototype>[] _types = { "Blunt", "Slash", "Piercing", "Heat", "Cold" };

    private void SubscribeTrauma()
    {
        Subs.CVar(_cfg, TraumaCVars.ExplosionLimbDamageVariation, x => PartVariation = x, true);
        Subs.CVar(_cfg, TraumaCVars.ExplosionWoundMultiplier, x => WoundMultiplier = x, true);
    }

    /// <summary>
    /// Applies explosion wound-severity multipliers to the damage specification.
    /// </summary>
    public void ModifyWoundSeverities(DamageSpecifier damage)
    {
        if (damage.PartDamageVariation == 0f)
            damage.PartDamageVariation = PartVariation;
        foreach (var type in _types)
        {
            damage.WoundSeverityMultipliers.TryAdd(type, WoundMultiplier);
        }
    }

    private void DamageBody(Entity<BodyComponent> ent, DamageSpecifier damage, string explosion)
    {
        var parts = _body.GetExternalOrgans(ent.AsNullable());
        var multipliers = _damageableSystem.GetDamageVariationMultipliers(ent, damage.PartDamageVariation, parts.Count);
        for (var i = 0; i < parts.Count; i++)
        {
            var part = parts[i];
            var resistanceEv = new GetExplosionResistanceEvent(explosion);
            RaiseLocalEvent(part, ref resistanceEv);
            resistanceEv.DamageCoefficient = Math.Max(0, resistanceEv.DamageCoefficient);

            var partDamage = resistanceEv.DamageCoefficient.Equals(1f)
                ? damage
                : damage * resistanceEv.DamageCoefficient;

            partDamage *= multipliers[i];
            _damageableSystem.ChangeDamage(part.Owner, partDamage, ignoreResistances: true, ignoreGlobalModifiers: true);
        }
    }
}
