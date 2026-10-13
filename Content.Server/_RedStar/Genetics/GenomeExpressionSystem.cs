using System.Linq;
using Content.Shared._RedStar.Xenobiology;
using Content.Shared.EntityEffects;
using Content.Shared.EntityEffects.Effects;
using Robust.Shared.Prototypes;

namespace Content.Server._RedStar.Genetics;

/// <summary>
/// Applies existing cell trait effects when their genetic conditions hold.
/// Only components created by this system are removed when expression ends.
/// </summary>
public sealed partial class GenomeExpressionSystem : EntitySystem
{
    [Dependency] private GenomeSystem _genomes = default!;
    [Dependency] private IPrototypeManager _prototypes = default!;
    [Dependency] private SharedEntityEffectsSystem _effects = default!;

    [SubscribeLocalEvent]
    private void OnGenomeChanged(Entity<GenomeComponent> ent, ref GenomeChangedEvent args) => Recalculate(ent.Owner);

    [SubscribeLocalEvent]
    private void OnGenomeReady(Entity<GenomeComponent> ent, ref GenomeReadyEvent args) => Recalculate(ent.Owner);

    [SubscribeLocalEvent]
    private void OnComponentInit(Entity<GenomeExpressionComponent> ent, ref ComponentInit args)
    {
        ent.Comp.Applied = [..ent.Comp.Applied];
        ent.Comp.OwnedComponents = ent.Comp.OwnedComponents.ToDictionary(entry => entry.Key,
            entry => new HashSet<string>(entry.Value));
    }

    private void Recalculate(EntityUid uid)
    {
        if (!_genomes.TryGetSnapshot(uid, out var genome) || genome is null)
            return;

        var rules = _prototypes.EnumeratePrototypes<CellTraitPrototype>().ToArray();
        var active = new HashSet<ProtoId<CellTraitPrototype>>();
        foreach (var rule in rules)
        {
            if (genome.TryGetGene(rule.Gene, out var state) &&
                (rule.Allele is null || state?.Allele == rule.Allele))
                active.Add(rule.ID);
        }

        if (active.Count == 0 && !HasComp<GenomeExpressionComponent>(uid))
            return;
        var host = EnsureComp<GenomeExpressionComponent>(uid);

        foreach (var rule in rules)
        {
            if (!host.Applied.Contains(rule.ID) || active.Contains(rule.ID))
                continue;

            // Non-component effects are deliberately left in place; their reversal is not generic.
            if (host.OwnedComponents.Remove(rule.ID, out var owned))
            {
                foreach (var name in owned)
                {
                    if (rules.Any(other => other.ID != rule.ID && active.Contains(other.ID) &&
                        other.Effects.OfType<AddComponents>().Any(effect => effect.Components.ContainsKey(name))))
                        continue;
                    if (Factory.TryGetRegistration(name, out var registration))
                        RemComp(uid, registration.Type);
                }
            }

            host.Applied.Remove(rule.ID);
        }

        foreach (var rule in rules)
        {
            if (!active.Contains(rule.ID) || !host.Applied.Add(rule.ID))
                continue;

            var owned = new HashSet<string>();
            foreach (var effect in rule.Effects.OfType<AddComponents>())
            {
                foreach (var name in effect.Components.Keys)
                {
                    if (Factory.TryGetRegistration(name, out var registration) &&
                        !HasComp(uid, registration.Type))
                        owned.Add(name);
                }
            }

            _effects.ApplyEffects(uid, rule.Effects);
            host.OwnedComponents[rule.ID] = owned;
        }
    }
}
