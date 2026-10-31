using Content.Server.Construction;
using Content.Server._RedStar.Beekeeping.Data;
using Content.Shared.DoAfter;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared._RedStar.Beekeeping.Components;
using Content.Shared._RedStar.Beekeeping.Data;
using Content.Shared._RedStar.Beekeeping.Events;
using Robust.Shared.Spawners;

namespace Content.Server._RedStar.Beekeeping.Systems;

/// <summary>
/// Bees as items. Taking one moves it from <see cref="BeehiveComponent.Bees"/> to
/// <see cref="BeehiveComponent.OutBees"/>; bringing it back to its own hive moves it back. A bee that dies
/// outside (lifetime over, killed, ground up, anything that deletes it) only frees its reserve and is lost:
/// the colony regrows it from nectar. The hive has one scout; while it is out or dead, the hive finds no new
/// places and no second scout can be taken.
/// </summary>
public sealed partial class BeehiveSystem
{
    [Dependency] private SharedHandsSystem _hands = default!;

    #region Taking

    private void StartTakeBee(Entity<BeehiveComponent> hive, EntityUid user, BeeRole role)
    {
        if (!CanTakeBee(hive, user, role, true))
            return;

        _doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager, user, hive.Comp.TakeDelay,
            new BeeTakeDoAfterEvent(role), hive, target: hive)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            NeedHand = true,
        });
    }

    private void StartTakeQueen(Entity<BeehiveComponent> hive, EntityUid user)
    {
        _doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager, user, hive.Comp.TakeDelay,
            new BeeTakeQueenDoAfterEvent(), hive, target: hive)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            NeedHand = true,
        });
    }

    [SubscribeLocalEvent]
    private void OnTakeQueenDoAfter(Entity<BeehiveComponent> ent, ref BeeTakeQueenDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        args.Handled = true;

        // Someone may have taken her in the meantime.
        if (!ItemSlots.TryGetSlot(ent.Owner, ent.Comp.QueenSlot, out var slot) ||
            slot.Item is not { } queen ||
            !_actionBlocker.CanPickup(args.User, queen) ||
            !ItemSlots.TryEjectToHands(ent, slot, args.User, excludeUserAudio: true))
        {
            return;
        }

        _popup.PopupEntity(Loc.GetString("beekeeping-take-queen-done"), ent, args.User);
    }

    private bool CanTakeBee(Entity<BeehiveComponent> hive, EntityUid user, BeeRole role, bool popup)
    {
        var reason = TakeBeeBlocker(hive.Comp, role);
        if (reason == null)
            return true;

        if (popup)
            _popup.PopupEntity(Loc.GetString(reason), hive, user);
        return false;
    }

    /// <summary>
    /// Why a bee of this role cannot be taken right now, as a locale id, or null if it can.
    /// </summary>
    private string? TakeBeeBlocker(BeehiveComponent comp, BeeRole role)
    {
        if (comp.OutBees - comp.AngryOut >= comp.MaxOutBees)
            return "beekeeping-take-too-many";

        if (role == BeeRole.Scout && (comp.ScoutState != BeeScoutState.Home || comp.Bees <= 0))
            return "beekeeping-take-no-scout";

        if (role == BeeRole.Scout && comp.ScoutFlyer is { } flyer && !TerminatingOrDeleted(flyer))
            return "beekeeping-take-scout-flying";

        if (role == BeeRole.Scout && Timing.CurTime < comp.ScoutRestsUntil)
            return "beekeeping-take-scout-tired";

        if (role == BeeRole.Pollinator)
        {
            if (WorkersAtHome(comp) <= 0)
                return "beekeeping-take-no-workers";

            comp.RestingWorkers.RemoveAll(until => until <= Timing.CurTime);
            if (WorkersAtHome(comp) <= comp.RestingWorkers.Count)
                return "beekeeping-take-workers-tired";
        }

        return null;
    }

    [SubscribeLocalEvent]
    private void OnTakeDoAfter(Entity<BeehiveComponent> ent, ref BeeTakeDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        args.Handled = true;

        // The hive may have changed during the do-after: check again.
        if (!CanTakeBee(ent, args.User, args.Role, true))
            return;

        var comp = ent.Comp;
        var proto = args.Role == BeeRole.Scout ? comp.ScoutPrototype : comp.PollinatorPrototype;
        var bee = Spawn(proto, Transform(args.User).Coordinates);
        if (!TryComp<BeeItemComponent>(bee, out var item))
        {
            Log.Error($"{proto} has no {nameof(BeeItemComponent)}");
            Del(bee);
            return;
        }

        comp.Bees--;
        comp.OutBees++;
        if (args.Role == BeeRole.Scout)
        {
            comp.ScoutState = BeeScoutState.Outside;
            _scout.RecallScout(comp);
        }

        item.SourceHive = ent;
        _queen.GiveTraits(ent, (bee, item));
        item.CountedOutside = true;

        // With no free hand it lands on the floor and flies straight back in.
        _hands.PickupOrDrop(args.User, bee);
        if (!_container.IsEntityInContainer(bee))
            _errands.FlyHome((bee, item));
        UpdateAmbience(ent);
        _popup.PopupEntity(Loc.GetString(args.Role == BeeRole.Scout
            ? "beekeeping-take-scout-done"
            : "beekeeping-take-pollinator-done"), ent, args.User);
    }

    #endregion

    #region Returning

    // The engine wants the same ordering for every InteractUsingEvent subscription of one system, and the
    // frame one runs before construction. A bee is no construction step, so the order changes nothing here.
    [SubscribeLocalEvent(before: [typeof(ConstructionSystem)])]
    private void OnHiveInteractUsing(Entity<BeehiveComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled || !TryComp<BeeItemComponent>(args.Used, out var bee))
            return;

        args.Handled = true;
        TryReturnBee(ent, (args.Used, bee), args.User);
    }

    /// <summary>
    /// Brings a bee back into its own hive, by hand or flying home on its own (no user then, no popups).
    /// All checks come first and change nothing; only then the bee is uncounted, moved back into the hive, its
    /// memory handed over, and the item deleted.
    /// </summary>
    public bool TryReturnBee(Entity<BeehiveComponent> hive, Entity<BeeItemComponent> bee, EntityUid? user)
    {
        string? reason = null;

        if (!bee.Comp.CountedOutside || bee.Comp.SourceHive != hive.Owner)
            reason = "beekeeping-return-wrong-hive";
        else if (TerminatingOrDeleted(hive))
            reason = "beekeeping-return-wrong-hive";
        else if (!HasRoomForReturn(hive.Comp))
            reason = "beekeeping-return-crowded";

        if (reason != null)
        {
            if (user != null)
                _popup.PopupEntity(Loc.GetString(reason), hive, user.Value);
            return false;
        }

        var comp = hive.Comp;
        bee.Comp.CountedOutside = false;
        ReleaseOutBee(hive);
        comp.Bees++;
        if (bee.Comp.Role == BeeRole.Scout)
        {
            comp.ScoutState = BeeScoutState.Home;
            MergeScoutMemory(hive, bee.Comp);
            if (bee.Comp.Worked)
                comp.ScoutRestsUntil = Timing.CurTime + comp.WorkedRest;
        }
        else if (bee.Comp.Worked)
        {
            comp.RestingWorkers.Add(Timing.CurTime + comp.WorkedRest);
        }

        UpdateAmbience(hive);
        if (user != null)
            _popup.PopupEntity(Loc.GetString("beekeeping-return-done"), hive, user.Value);
        QueueDel(bee);
        return true;
    }

    /// <summary>
    /// A bee may come back while the hive is within its limit, and also during the grace period after frames
    /// were taken out, so a normal harvest does not cost bees. After the grace period it is refused.
    /// </summary>
    private bool HasRoomForReturn(BeehiveComponent comp)
    {
        if (comp.Bees + comp.OutBees <= comp.MaxBees)
            return true;

        return comp.CrowdedSince == null || Timing.CurTime - comp.CrowdedSince.Value < comp.CrowdingGrace;
    }

    /// <summary>
    /// Hands the trays a scout remembered to its hive. Trays that are gone or on another grid are dropped; the rest
    /// go through the same checks as any visit, so far or hidden trays start inactive.
    /// </summary>
    private void MergeScoutMemory(Entity<BeehiveComponent> hive, BeeItemComponent bee)
    {
        var now = Timing.CurTime;
        var xform = Transform(hive);
        var hiveMap = _transform.GetMapCoordinates(hive, xform);

        foreach (var tray in bee.Memory)
        {
            if (TerminatingOrDeleted(tray) || Transform(tray).GridUid != xform.GridUid)
                continue;

            if (_scout.AddSource(hive, tray, now) is not { } source)
                continue;

            switch (_scout.CheckSource(hive, xform, hiveMap, source, out _))
            {
                case BeeSourceCheck.Invalid:
                    hive.Comp.KnownSources.Remove(source);
                    break;
                case BeeSourceCheck.Inactive:
                    source.Active = false;
                    source.NextVisit = now + hive.Comp.InactiveRecheck;
                    break;
            }
        }

        bee.Memory.Clear();
        bee.MemoryCount = 0;
    }

    #endregion

    #region Lifetime

    [SubscribeLocalEvent]
    private void OnBeeMapInit(Entity<BeeItemComponent> ent, ref MapInitEvent args)
    {
        if (TryComp<TimedDespawnComponent>(ent, out var despawn))
        {
            ent.Comp.ExpiresAt = Timing.CurTime + TimeSpan.FromSeconds(despawn.Lifetime);
            Dirty(ent);
        }
    }

    [SubscribeLocalEvent]
    private void OnBeeExpired(Entity<BeeItemComponent> ent, ref TimedDespawnEvent args)
    {
        ent.Comp.Expired = true;
    }

    /// <summary>
    /// Any way the bee item goes away frees its reserve in the hive, once. A returned bee was uncounted before
    /// it was deleted, so this does nothing for it.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnBeeTerminating(Entity<BeeItemComponent> ent, ref EntityTerminatingEvent args)
    {
        if (!ent.Comp.CountedOutside)
            return;

        ent.Comp.CountedOutside = false;

        if (ent.Comp.SourceHive is not { } hiveUid ||
            TerminatingOrDeleted(hiveUid) ||
            !TryComp<BeehiveComponent>(hiveUid, out var comp))
        {
            return;
        }

        var hive = new Entity<BeehiveComponent>(hiveUid, comp);
        ReleaseOutBee(hive);

        if (ent.Comp.Role == BeeRole.Scout)
        {
            // Lifetime over: the delay has already passed, a worker takes over at once.
            // Killed or destroyed earlier: a new scout after the full delay.
            comp.ScoutState = BeeScoutState.Lost;
            comp.ScoutReplaceAt = Timing.CurTime + (ent.Comp.Expired ? TimeSpan.Zero : comp.ScoutReplaceDelay);
            if (ent.Comp.Expired && comp.Bees >= comp.ScoutReplaceMinBees)
                comp.ScoutState = BeeScoutState.Home;
        }

    }

    public void ReleaseOutBee(Entity<BeehiveComponent> hive)
    {
        if (hive.Comp.OutBees <= 0)
        {
            Log.Warning($"{ToPrettyString(hive)} released a bee from outside while counting none outside.");
            hive.Comp.OutBees = 0;
            return;
        }

        hive.Comp.OutBees--;
    }

    #endregion
}
