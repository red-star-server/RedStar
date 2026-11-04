using Content.Server._RedStar.Beekeeping.Components;
using Content.Shared._RedStar.Beekeeping.Components;
using Content.Shared._RedStar.Beekeeping.Data;
using Content.Shared._RedStar.Beekeeping.Events;
using Content.Shared.Body.Systems;
using Content.Shared.Botany.Components;
using Content.Shared.Botany.Events;
using Content.Shared.Botany.Systems;
using Content.Shared.Damage.Systems;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.FixedPoint;
using Content.Shared.Hands;
using Content.Shared.Interaction;
using Content.Shared.Inventory;
using Content.Shared.Popups;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._RedStar.Beekeeping.Systems;

/// <summary>
/// A bee in hand applied to a plant tray or the plant in it: a scout remembers the tray's tile, a pollinator
/// pollinates the plant. Pollination rolls the harvest bonus at once and puts a <see cref="BeePollinatedComponent"/>
/// on the plant; the marker raises <c>YieldMod</c> and is removed after the next harvest, taking back what it added.
/// No botany code is changed: the marker watches the plant from outside. An aggressive bee picked up bites or stings.
/// </summary>
public sealed partial class BeeItemSystem : EntitySystem
{
    [Dependency] private BeehiveSystem _hive = default!;
    [Dependency] private BloodstreamSystem _bloodstream = default!;
    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private InventorySystem _inventory = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private PlantHolderSystem _plantHolder = default!;
    [Dependency] private PlantTraySystem _plantTray = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private IGameTiming _timing = default!;

    [Dependency] private EntityQuery<PlantHolderComponent> _holderQuery;
    [Dependency] private EntityQuery<PlantHarvestComponent> _harvestQuery;
    [Dependency] private EntityQuery<PlantTrayComponent> _trayQuery;

    /// <summary>
    /// How often pollination markers are checked for running out or a dead plant. A harvest removes the marker
    /// at once.
    /// </summary>
    private static readonly TimeSpan MarkerCheckInterval = TimeSpan.FromSeconds(1);

    private TimeSpan _nextMarkerCheck;
    private readonly List<Entity<BeePollinatedComponent, PlantHolderComponent>> _expiredMarkers = new();

    #region Applying

    [SubscribeLocalEvent]
    private void OnAfterInteract(Entity<BeeItemComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Handled ||
            !args.CanReach ||
            args.Target is not { } target ||
            !TryGetTray(target, out var tray))
        {
            return;
        }

        args.Handled = true;

        if (!CanApply(ent, tray, args.User))
            return;

        _doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager, args.User, ent.Comp.ApplyDelay,
            new BeeApplyDoAfterEvent(), ent, target: tray, used: ent)
        {
            BreakOnMove = true,
            NeedHand = true
        });
    }

    [SubscribeLocalEvent]
    private void OnApply(Entity<BeeItemComponent> ent, ref BeeApplyDoAfterEvent args)
    {
        if (args.Cancelled ||
            args.Handled ||
            args.Target is not { } target ||
            !TryGetTray(target, out var tray))
        {
            return;
        }

        args.Handled = true;

        // The plant may have changed during the do-after: check again.
        if (!CanApply(ent, tray, args.User))
            return;

        switch (ent.Comp.Role)
        {
            case BeeRole.Scout:
                Remember(ent, tray, args.User);
                break;
            case BeeRole.Pollinator:
                if (GetPlant(tray) is { } plant)
                    Pollinate(ent, tray, plant, args.User);
                break;
        }
    }

    /// <summary>
    /// The tray behind a click: the tray itself, or the tray holding the clicked plant.
    /// </summary>
    private bool TryGetTray(EntityUid target, out Entity<PlantTrayComponent> tray)
    {
        if (_trayQuery.TryComp(target, out var trayComp))
        {
            tray = (target, trayComp);
            return true;
        }

        var parent = Transform(target).ParentUid;
        if (_holderQuery.HasComp(target) &&
            _trayQuery.TryComp(parent, out trayComp) &&
            trayComp.PlantEntity == target)
        {
            tray = (parent, trayComp);
            return true;
        }

        tray = default;
        return false;
    }

    private Entity<PlantHolderComponent>? GetPlant(Entity<PlantTrayComponent> tray)
    {
        if (tray.Comp.PlantEntity is not { } plant ||
            TerminatingOrDeleted(plant) ||
            !_holderQuery.TryComp(plant, out var holder))
        {
            return null;
        }

        return (plant, holder);
    }

    private bool CanApply(Entity<BeeItemComponent> bee, Entity<PlantTrayComponent> tray, EntityUid user)
    {
        string? reason = null;

        if (bee.Comp.Role == BeeRole.Scout)
        {
            if (Transform(tray).GridUid == null)
                reason = "beekeeping-scout-no-grid";
        }
        else
        {
            var plant = GetPlant(tray);
            if (bee.Comp.Charges <= 0)
                reason = "beekeeping-pollinate-no-charges";
            else if (plant == null)
                reason = "beekeeping-pollinate-no-plant";
            else if (plant.Value.Comp.Dead)
                reason = "beekeeping-pollinate-dead";
            else if (_harvestQuery.TryComp(plant.Value, out var harvest) &&
                     harvest.HarvestRepeat == HarvestType.SelfHarvest)
                reason = "beekeeping-pollinate-self-harvest";
            else if (plant.Value.Comp.ReadyForHarvest)
                reason = "beekeeping-pollinate-ripe";
            else if (HasComp<BeePollinatedComponent>(plant.Value))
                reason = "beekeeping-pollinate-already";
        }

        if (reason == null)
            return true;

        _popup.PopupEntity(Loc.GetString(reason), tray, user);
        return false;
    }

    /// <summary>
    /// The scout remembers the tray. A tray it already knows takes no new slot; past the limit the oldest one is
    /// forgotten. The hive learns the trays only when the scout is brought back.
    /// </summary>
    private void Remember(Entity<BeeItemComponent> bee, EntityUid tray, EntityUid user)
    {
        if (Transform(tray).GridUid == null)
            return;

        var memory = bee.Comp.Memory;
        if (memory.Contains(tray))
        {
            _popup.PopupEntity(Loc.GetString("beekeeping-scout-known"), tray, user);
            return;
        }

        if (bee.Comp.MemoryLimit <= 0)
            return;

        while (memory.Count >= bee.Comp.MemoryLimit)
        {
            memory.RemoveAt(0);
        }

        memory.Add(tray);
        bee.Comp.MemoryCount = memory.Count;
        bee.Comp.Worked = true;
        Dirty(bee);

        _popup.PopupEntity(Loc.GetString("beekeeping-scout-remembered",
            ("count", memory.Count), ("max", bee.Comp.MemoryLimit)), tray, user);
    }

    /// <summary>
    /// The mutation table unstable mutagen feeds; mutagenic pollination feeds the same one.
    /// </summary>
    private static readonly ProtoId<RandomPlantMutationListPrototype> MutationTable = "RandomPlantMutations";

    /// <summary>
    /// Rolls the harvest bonus now. The plant is marked as pollinated either way, so the roll cannot be
    /// repeated on the same plant by spending more charges. Pests and weeds in the tray go down with any result.
    /// </summary>
    private void Pollinate(Entity<BeeItemComponent> bee,
        Entity<PlantTrayComponent> tray,
        Entity<PlantHolderComponent> plant,
        EntityUid user)
    {
        var marker = EnsureComp<BeePollinatedComponent>(plant);
        marker.Bonus = _random.Prob(bee.Comp.DoubleYieldChance);

        // The bonus raises the yield to the bonus level; only what was added is remembered and later taken back.
        if (marker.Bonus)
        {
            marker.AddedYieldMod = Math.Max(0, bee.Comp.BonusYieldMod - plant.Comp.YieldMod);
            _plantHolder.AdjustsYieldMod(plant.AsNullable(), marker.AddedYieldMod);
        }

        if (bee.Comp.PestControl)
        {
            _plantTray.AdjustPest(tray.AsNullable(), -bee.Comp.PestReduction);
            _plantTray.AdjustWeed(tray.AsNullable(), -bee.Comp.WeedReduction);
        }

        // A mutagenic colony stirs the plant's genes.
        if (bee.Comp.PollinationMutation > 0f)
            _plantHolder.AdjustsMutationLevel(plant.AsNullable(), MutationTable, bee.Comp.PollinationMutation);

        marker.ExpiresAt = _timing.CurTime + bee.Comp.PollinationDuration;

        bee.Comp.Charges--;
        bee.Comp.Worked = true;
        Dirty(bee);

        _popup.PopupEntity(Loc.GetString(marker.Bonus
            ? "beekeeping-pollinate-bonus"
            : "beekeeping-pollinate-no-bonus"), tray, user);
    }

    #endregion

    #region Marker

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;
        if (now < _nextMarkerCheck)
            return;

        _nextMarkerCheck = now + MarkerCheckInterval;
        _expiredMarkers.Clear();

        var query = EntityQueryEnumerator<BeePollinatedComponent, PlantHolderComponent>();
        while (query.MoveNext(out var uid, out var marker, out var plant))
        {
            if (plant.Dead || now >= marker.ExpiresAt)
                _expiredMarkers.Add((uid, marker, plant));
        }

        foreach (var ent in _expiredMarkers)
        {
            EndPollination(ent);
        }
    }

    /// <summary>
    /// The harvest used up the pollination.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnPollinatedHarvested(Entity<BeePollinatedComponent> ent, ref PlantHarvestedEvent args)
    {
        if (!_holderQuery.TryComp(ent, out var plant))
            return;

        EndPollination((ent, ent.Comp, plant));
    }

    /// <summary>
    /// Takes back what the pollination added to <c>YieldMod</c>, keeping any other change to it.
    /// </summary>
    private void EndPollination(Entity<BeePollinatedComponent, PlantHolderComponent> ent)
    {
        if (TerminatingOrDeleted(ent))
            return;

        var plant = ent.Comp2;
        if (ent.Comp1.AddedYieldMod != 0)
            _plantHolder.AdjustsYieldMod((ent, plant), -ent.Comp1.AddedYieldMod);

        RemComp<BeePollinatedComponent>(ent);
    }

    [SubscribeLocalEvent]
    private void OnPollinatedExamined(Entity<BeePollinatedComponent> ent, ref ExaminedEvent args)
    {
        args.PushMarkup(Loc.GetString(ent.Comp.Bonus
            ? "beekeeping-plant-examine-pollinated-bonus"
            : "beekeeping-plant-examine-pollinated"));
    }

    #endregion

    /// <summary>
    /// A hand picks up an aggressive bee. Without a sting it bites a bare hand; with a sting it stings through any
    /// gloves except sting-proof ones, and injects its payload. Not in water vapor.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnBeePickedUp(Entity<BeeItemComponent> ent, ref GotEquippedHandEvent args)
    {
        var totals = _hive.GetTotals(ent.Comp.Traits);
        if (!totals.Aggressive)
            return;

        var user = args.User;

        // In water vapor even an aggressive bee keeps calm.
        if (_hive.InCalmingVapor(user))
            return;

        _inventory.TryGetSlotEntity(user, "gloves", out var gloves);

        if (!totals.Sting)
        {
            if (gloves != null || ent.Comp.BiteDamage == null)
                return;

            _damageable.TryChangeDamage(user, ent.Comp.BiteDamage, origin: ent);
            _popup.PopupEntity(Loc.GetString("beekeeping-bee-bite"), user, user, PopupType.SmallCaution);
            return;
        }

        if (gloves is { } worn &&
            MetaData(worn).EntityPrototype is { } glovesProto &&
            ent.Comp.StingProofGloves.Contains(glovesProto.ID))
        {
            return;
        }

        if (ent.Comp.StingDamage != null)
            _damageable.TryChangeDamage(user, ent.Comp.StingDamage, origin: ent);

        if (ent.Comp.Payload.Volume > 0)
        {
            var dose = ent.Comp.Payload.SplitSolution(FixedPoint2.Min(ent.Comp.PayloadPerSting, ent.Comp.Payload.Volume));
            _bloodstream.TryAddToBloodstream(user, dose);
        }

        _popup.PopupEntity(Loc.GetString("beekeeping-bee-sting"), user, user, PopupType.SmallCaution);
    }
}
