using Content.Server.NPC.HTN;
using Content.Server.NPC.Systems;
using Content.Server._RedStar.Beekeeping.Components;
using Content.Server._RedStar.Beekeeping.Data;
using Content.Server._RedStar.Beekeeping.Events;
using Content.Shared.Audio;
using Content.Shared.Damage.Systems;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Item;
using Content.Shared.Mobs;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Popups;
using Content.Shared.Tools.Systems;
using Content.Shared._RedStar.Beekeeping.Components;
using Content.Shared._RedStar.Beekeeping.Visuals;
using Robust.Shared.Configuration;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server._RedStar.Beekeeping.Systems;

/// <summary>
/// Caught by hand, hurt, killed or lost on the way: what the colony makes of it.
/// </summary>
public sealed partial class BeeErrandSystem
{
    /// <summary>
    /// A bee in the air is picked up like any small creature: a worker on its way to a flower, or a bee flying home. Not
    /// while it is inside a flower, a vent or the hive, and not the scout on its trip or a bee on any other errand.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnPickupAttempt(Entity<BeeErrandComponent> ent, ref GettingPickedUpAttemptEvent args)
    {
        if (ent.Comp.Act != BeeErrandAct.None || ent.Comp.Kind is not (BeeErrandKind.Forage or BeeErrandKind.Return))
            args.Cancel();
    }

    /// <summary>
    /// Held or packed away, a bee leaves its errand, keeps still and buzzes quietly.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnBeeContained(Entity<BeeItemComponent> ent, ref EntGotInsertedIntoContainerMessage args)
    {
        if (TryComp<BeeErrandComponent>(ent, out var errand))
            Catch(ent, (ent, errand), args.Container.Owner);

        _npc.SleepNPC(ent);
        SetHeldSound(ent, true);
    }

    /// <summary>
    /// Let go, a bee flies home by itself.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnBeeLetGo(Entity<BeeItemComponent> ent, ref EntGotRemovedFromContainerMessage args)
    {
        if (TerminatingOrDeleted(ent) || _container.IsEntityInContainer(ent))
            return;

        SetHeldSound(ent, false);
        FlyHome(ent);
    }

    /// <summary>
    /// A free bee taken out of a hive flies back into it; one with no hive to go to flies about until its time is up.
    /// </summary>
    public void FlyHome(Entity<BeeItemComponent> bee)
    {
        if (bee.Comp.CountedOutside && bee.Comp.SourceHive is { } hive && !TerminatingOrDeleted(hive))
        {
            SendHome(bee, hive);
            return;
        }

        if (TryComp<HTNComponent>(bee, out var htn))
            _npc.WakeNPC(bee, htn);
    }

    /// <summary>
    /// The bee leaves its errand without being lost. A worker caught on its way is counted outside its hive, like one
    /// taken out.
    /// </summary>
    private void Catch(Entity<BeeItemComponent> bee, Entity<BeeErrandComponent> errand, EntityUid holder)
    {
        var hiveUid = errand.Comp.Hive;
        if (!TerminatingOrDeleted(hiveUid) && TryComp<BeehiveComponent>(hiveUid, out var comp))
        {
            comp.Foragers.Remove(bee);
            if (errand.Comp.Kind == BeeErrandKind.Forage && !bee.Comp.CountedOutside && comp.Bees > 0)
            {
                var hive = new Entity<BeehiveComponent>(hiveUid, comp);
                comp.Bees--;
                comp.OutBees++;
                bee.Comp.SourceHive = hive;
                bee.Comp.CountedOutside = true;
                _popup.PopupEntity(Loc.GetString("beekeeping-forager-caught"), holder, holder);
            }
        }

        errand.Comp.Ended = true;
        Hide(bee, false);
        SetSize(bee, BeeFlightSize.Full);
        RemComp<BeeErrandComponent>(bee);
    }

    private void SetHeldSound(Entity<BeeItemComponent> bee, bool held)
    {
        if (!TryComp<AmbientSoundComponent>(bee, out var ambient))
            return;

        bee.Comp.FreeSound ??= ambient.Sound;
        var sound = held ? bee.Comp.HeldSound : bee.Comp.FreeSound;
        if (ambient.Sound != sound)
            _ambient.SetSound(bee, sound, ambient);
    }

    /// <summary>
    /// Someone hurt a forager or the scout: the colony may send angry bees after them (the angry hive decides).
    /// </summary>
    [SubscribeLocalEvent]
    private void OnDamaged(Entity<BeeErrandComponent> ent, ref DamageDealtEvent args)
    {
        if (ent.Comp.Kind is not (BeeErrandKind.Forage or BeeErrandKind.Scout) ||
            args.Origin is not { } attacker ||
            !args.Damage.AnyPositive() ||
            TerminatingOrDeleted(ent.Comp.Hive))
        {
            return;
        }

        var ev = new BeeErrandAttackedEvent(ent, ent.Comp.Hive, attacker);
        RaiseLocalEvent(ref ev);
    }

    /// <summary>
    /// A killed bee is gone at once; it never makes it home.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnMobStateChanged(Entity<BeeErrandComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState == MobState.Dead)
            QueueDel(ent);
    }

    /// <summary>
    /// A bee that goes without its errand being over (killed, lost in the pipes, out of time looking for its way) is a
    /// worker the colony loses. The scout keeps its place; only what it found is lost. A swarm's bees left the colony when
    /// it set off.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnTerminating(Entity<BeeErrandComponent> ent, ref EntityTerminatingEvent args)
    {
        if (ent.Comp.Ended || TerminatingOrDeleted(ent.Comp.Hive) || !TryComp<BeehiveComponent>(ent.Comp.Hive, out var hive))
            return;

        ent.Comp.Ended = true;
        hive.Foragers.Remove(ent);
        if (ent.Comp.Kind is BeeErrandKind.Forage or BeeErrandKind.Hunt)
            _hive.RemoveHomeBee((ent.Comp.Hive, hive));

        var ev = new BeeErrandLostEvent(ent, ent.Comp.Hive, ent.Comp.Kind);
        RaiseLocalEvent(ref ev);
    }
}
