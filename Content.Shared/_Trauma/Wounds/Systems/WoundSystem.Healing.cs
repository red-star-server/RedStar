using System.Linq;
using Content.Shared._Trauma.Traumas;
using Content.Shared.Body;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.FixedPoint;
using Content.Shared.Rejuvenate;
using Robust.Shared.Prototypes;

namespace Content.Shared._Trauma.Wounds;

/// <summary>
/// This class is responsible for managing wound healing in the shared game code.
/// It contains methods for halting all bleeding on a given entity.
/// </summary>
public partial class WoundSystem
{
    [SubscribeLocalEvent]
    private void OnRejuvenate(Entity<WoundableComponent> ent, ref RejuvenateEvent args)
    {
        _container.CleanContainer(ent.Comp.Wounds);
        ent.Comp.Bleeds = FixedPoint2.Zero;
        DirtyField(ent, ent.Comp, nameof(WoundableComponent.Bleeds));
        UpdateWoundableIntegrity(ent.AsNullable());
        CheckWoundableSeverityThresholds(ent.AsNullable());
    }

    #region Public API

    /// <summary>
    /// Heals bleeding wounds on a body entity, starting with the most severely bleeding woundable
    /// and cascading any leftover healing to the next most severe bleeding woundable.
    /// </summary>
    /// <param name="body">The body entity to check for bleeding wounds</param>
    /// <param name="healAmount">The amount of healing to apply</param>
    /// <param name="healed">The total amount of bleeding that was healed</param>
    /// <param name="component">Optional body component if already resolved</param>
    /// <returns>True if any bleeding was healed, false otherwise</returns>
    public bool TryHealMostSevereBleedingWoundables(EntityUid body, float healAmount, out FixedPoint2 healed, BodyComponent? component = null)
    {
        healed = FixedPoint2.Zero;
        if (!Resolve(body, ref component) || healAmount == 0)
            return false;

        if (healAmount < 0)
        {
            Log.Error($"Negative healing amount {healAmount} passed to heal {ToPrettyString(body)}, Stack trace: {Environment.StackTrace}");
            return false;
        }

        var bleedingWoundables = new List<(EntityUid Woundable, FixedPoint2 BleedAmount)>();
        foreach (var part in _body.GetOrgans<WoundableComponent>(body))
        {
            var totalBleedAmount = FixedPoint2.Zero;
            var hasBleedingWounds = false;
            foreach (var wound in GetWoundableWounds(part.AsNullable()))
            {
                if (!_bleedQuery.TryComp(wound, out var bleeds) || !bleeds.IsBleeding)
                    continue;

                hasBleedingWounds = true;
                totalBleedAmount += bleeds.BleedingAmount;
            }

            if (hasBleedingWounds)
                bleedingWoundables.Add((part.Owner, totalBleedAmount));
        }

        var sortedWoundables = bleedingWoundables
            .OrderByDescending(x => x.BleedAmount)
            .Select(x => x.Woundable)
            .ToList();

        var remainingHealAmount = healAmount;
        var anyHealed = false;

        foreach (var woundable in sortedWoundables)
        {
            if (remainingHealAmount <= 0)
                break;

            var didHeal = TryHealBleedingWounds(woundable, remainingHealAmount, out var modifiedBleed);
            if (!didHeal)
                continue;

            anyHealed = true;
            healed += modifiedBleed;
            remainingHealAmount -= (float) modifiedBleed;
        }

        return anyHealed;
    }

    /// <summary>
    /// Attempts to reduce bleeding on the part and reports the applied change.
    /// </summary>
    public bool TryHealBleedingWounds(Entity<WoundableComponent?> part, FixedPoint2 bleedStopAbility, out FixedPoint2 modifiedBleed)
    {
        modifiedBleed = FixedPoint2.Zero;
        if (bleedStopAbility <= 0)
            return false;

        foreach (var wound in GetWoundableWounds(part))
        {
            if (!_bleedQuery.TryComp(wound, out var bleeds) || !bleeds.IsBleeding)
                continue;

            var previous = bleeds.BleedingAmount;
            if (previous <= 0)
                continue;

            var amount = FixedPoint2.Min(bleedStopAbility, previous);
            if (bleeds.Scaling <= FixedPoint2.Zero)
                continue;

            bleeds.BleedingAmountRaw = FixedPoint2.Max(bleeds.BleedingAmountRaw - amount / bleeds.Scaling, 0);
            var healed = previous - bleeds.BleedingAmount;
            modifiedBleed += healed;
            bleedStopAbility -= healed;

            if (bleeds.BleedingAmountRaw == 0)
            {
                bleeds.IsBleeding = false;
                bleeds.Scaling = 0;
            }

            DirtyFields(wound, bleeds, null, nameof(BleedInflicterComponent.BleedingAmountRaw),
                nameof(BleedInflicterComponent.IsBleeding), nameof(BleedInflicterComponent.Scaling));
            if (bleedStopAbility <= 0)
                break;
        }

        return modifiedBleed > FixedPoint2.Zero;
    }

    /// <summary>
    /// Attempts to heal eligible wounds on the part using the supplied healing budget.
    /// </summary>
    public bool TryHealWounds(Entity<WoundableComponent?> part,
        FixedPoint2 healAmount,
        out FixedPoint2 healed,
        [ForbidLiteral] ProtoId<DamageGroupPrototype>? damageGroup = null,
        bool ignoreBlockers = false)
    {
        healed = FixedPoint2.Zero;
        if (!_woundableQuery.Resolve(part, ref part.Comp) || healAmount == FixedPoint2.Zero)
            return false;

        if (healAmount < FixedPoint2.Zero)
        {
            Log.Error($"Negative healing amount {healAmount} passed to heal {ToPrettyString(part)} of group {damageGroup}, Stack trace: {Environment.StackTrace}");
            return false;
        }

        var woundsToHeal = new List<Entity<WoundComponent>>();
        foreach (var wound in GetWoundableWounds(part))
        {
            if (damageGroup != null && damageGroup != wound.Comp.DamageGroup ||
                !CanHealWound(wound, ignoreBlockers))
                continue;

            woundsToHeal.Add(wound);
        }

        if (woundsToHeal.Count == 0)
            return false;

        healed = SpreadHealing(woundsToHeal, healAmount);

        UpdateWoundableIntegrity(part);
        CheckWoundableSeverityThresholds(part);

        return healed > 0;
    }

    /// <summary>
    /// Attempts to heal wounds matching the requested damage type.
    /// </summary>
    public bool TryHealWoundsOfType(Entity<WoundableComponent?> part,
        FixedPoint2 healAmount,
        [ForbidLiteral] ProtoId<DamageTypePrototype> damageType,
        out FixedPoint2 healed,
        bool ignoreBlockers = false)
    {
        healed = 0;
        if (!_woundableQuery.Resolve(part, ref part.Comp) || healAmount == FixedPoint2.Zero)
            return false;

        if (healAmount < FixedPoint2.Zero)
        {
            Log.Error($"Negative healing amount {healAmount} passed to heal {ToPrettyString(part)} of type {damageType}, Stack trace: {Environment.StackTrace}");
            return false;
        }

        var woundsToHeal = new List<Entity<WoundComponent>>();
        foreach (var wound in GetWoundableWounds(part))
        {
            if (damageType != wound.Comp.DamageType ||
                !CanHealWound(wound, ignoreBlockers))
                continue;

            woundsToHeal.Add(wound);
        }

        if (woundsToHeal.Count == 0)
            return false;

        healed = SpreadHealing(woundsToHeal, healAmount);

        UpdateWoundableIntegrity(part);
        CheckWoundableSeverityThresholds(part);

        return healed > 0;
    }

    /// <summary>
    /// Attempts to heal eligible wounds on the part using the supplied healing budget.
    /// </summary>
    public bool TryHealWounds(Entity<WoundableComponent?> part,
        DamageSpecifier damage,
        out Dictionary<string, FixedPoint2> healed,
        bool ignoreBlockers = false)
    {
        healed = [];
        if (!_woundableQuery.Resolve(part, ref part.Comp))
            return false;

        foreach (var (type, amount) in damage.DamageDict)
        {
            if (!TryHealWoundsOfType(part, amount, type, out var typeHealed, ignoreBlockers)) continue;
            healed.Add(type, typeHealed);
        }

        return healed.Count != 0;
    }

    /// <summary>
    /// Checks whether the wound can be healed under the current healing restrictions.
    /// </summary>
    public bool CanHealWound(Entity<WoundComponent> wound, bool ignoreBlockers = false)
    {
        if (!ignoreBlockers && !wound.Comp.CanBeHealed)
            return false;

        var holdingWoundable = wound.Comp.HoldingWoundable;

        var ev = new WoundHealAttemptOnWoundableEvent(wound);
        RaiseLocalEvent(holdingWoundable, ref ev);

        if (ev.Cancelled)
            return false;

        var ev1 = new WoundHealAttemptEvent((holdingWoundable, _woundableQuery.Comp(holdingWoundable)), ignoreBlockers);
        RaiseLocalEvent(wound, ref ev1);

        return !ev1.Cancelled;
    }

    /// <summary>
    /// Method to get all wounds of some entity.
    /// The list is a fresh copy owned by the caller.
    /// </summary>
    public bool TryGetBodyWounds(EntityUid body, out List<Entity<WoundComponent>> wounds)
    {
        wounds = new List<Entity<WoundComponent>>();
        foreach (var part in _body.GetOrgans<WoundableComponent>(body))
        {
            AddWounds(part, wounds);
        }

        return wounds.Count > 0;
    }

    private void AddWounds(WoundableComponent part, List<Entity<WoundComponent>> wounds)
    {
        foreach (var wound in part.Wounds.ContainedEntities)
        {
            if (_query.TryComp(wound, out var comp))
                wounds.Add((wound, comp));
        }
    }

    /// <summary>
    /// Method to get all wounded parts of entity
    /// </summary>
    public bool TryGetBodyWoundedParts(EntityUid body, out List<Entity<WoundableComponent>> woundables)
    {
        woundables = [];

        foreach (var part in _body.GetOrgans<WoundableComponent>(body))
        {
            if (part.Comp.Wounds is { Count: > 0 })
                woundables.Add(part);
        }

        return woundables.Count > 0;
    }

    /// <summary>
    /// Method to heal all wounds on entity by specific healing amount.
    /// The healing of each damage type is spread evenly over all healable wounds of that type on the whole body,
    /// e.g. 10 Brute healing with 2 Brute wounds heals 5 on each, or more on one if the other is too small to take its share.
    /// </summary>
    public bool TryHealWoundsOnOwner(EntityUid body, DamageSpecifier healing, bool ignoreBlockers = false)
    {
        var healed = false;
        foreach (var (type, amount) in healing.DamageDict)
        {
            if (amount < FixedPoint2.Zero)
            {
                Log.Error($"Negative healing amount {amount} passed to heal {ToPrettyString(body)} of type {type}, Stack trace: {Environment.StackTrace}");
                continue;
            }

            if (amount == FixedPoint2.Zero)
                continue;

            var woundsToHeal = new List<Entity<WoundComponent>>();
            foreach (var part in _body.GetOrgans<WoundableComponent>(body))
            {
                foreach (var wound in GetWoundableWounds(part.AsNullable()))
                {
                    if (wound.Comp.DamageType.Id == type && CanHealWound(wound, ignoreBlockers))
                        woundsToHeal.Add(wound);
                }
            }

            if (woundsToHeal.Count == 0)
                continue;

            healed |= SpreadHealing(woundsToHeal, amount) > FixedPoint2.Zero;
        }

        return healed;
    }

    #endregion

    #region Private API

    /// <summary>
    /// Spreads a healing budget evenly over the given wounds.
    /// Smallest wounds go first so whatever they can't take is passed on to the bigger ones instead of being wasted.
    /// The list gets sorted. Returns the amount that was actually healed.
    /// </summary>
    private FixedPoint2 SpreadHealing(List<Entity<WoundComponent>> wounds, FixedPoint2 amount)
    {
        wounds.Sort(static (a, b) => a.Comp.WoundSeverityPoint.CompareTo(b.Comp.WoundSeverityPoint));

        var healed = FixedPoint2.Zero;
        var remaining = amount;
        for (var i = 0; i < wounds.Count && remaining > FixedPoint2.Zero; i++)
        {
            var share = remaining / (wounds.Count - i);
            var change = -ChangeWoundSeverity(wounds[i], -share);
            healed += change;
            remaining -= change;
        }

        return healed;
    }

    #endregion
}
