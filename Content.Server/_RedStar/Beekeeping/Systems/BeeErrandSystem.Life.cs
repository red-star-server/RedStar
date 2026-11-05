using Content.Server._RedStar.Beekeeping.Components;
using Content.Server._RedStar.Beekeeping.Data;
using Content.Server.NPC.HTN;
using Content.Shared._RedStar.Beekeeping.Components;
using Content.Shared._RedStar.Beekeeping.Visuals;
using Content.Shared.Audio;
using Content.Shared.Item;
using Content.Shared.Mobs;
using Robust.Shared.Containers;

namespace Content.Server._RedStar.Beekeeping.Systems;

/// <summary>
/// Caught by hand, hurt, killed or lost on the way: what the colony makes of it.
/// </summary>
public sealed partial class BeeErrandSystem
{
    /// <summary>
    /// A bee in the air is picked up like any small creature: a worker on its way to a flower, or a bee flying home. Not
    /// while it is inside a flower or the hive, and not the scout on its trip or a bee on any other errand.
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
            var hive = new Entity<BeehiveComponent>(hiveUid, comp);
            if (errand.Comp.Kind == BeeErrandKind.Forage && !bee.Comp.CountedOutside && _hive.MoveHomeBeeOutside(hive))
            {
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
    /// A killed bee is gone at once; it never makes it home.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnMobStateChanged(Entity<BeeErrandComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState == MobState.Dead)
            QueueDel(ent);
    }

    /// <summary>
    /// A bee that goes without its errand being over (killed, out of time looking for its way) is a worker the colony
    /// loses. The scout keeps its place; only what it found is lost.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnTerminating(Entity<BeeErrandComponent> ent, ref EntityTerminatingEvent args)
    {
        if (ent.Comp.Ended || TerminatingOrDeleted(ent.Comp.Hive) || !TryComp<BeehiveComponent>(ent.Comp.Hive, out var hive))
            return;

        ent.Comp.Ended = true;
        hive.Foragers.Remove(ent);
        if (ent.Comp.Kind == BeeErrandKind.Forage)
            _hive.RemoveHomeBee((ent.Comp.Hive, hive));
    }
}
