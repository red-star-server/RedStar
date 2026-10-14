using System.Linq;
using Content.Server._RedStar.Genetics;
using Content.Server.Storage.EntitySystems;
using Content.Shared.Body.Components;
using Content.Shared._RedStar.Genetics;
using Content.Server.Power.EntitySystems;
using Content.Shared._RedStar.Xenobiology.Components.Machines;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Mobs.Components;
using Content.Shared.Popups;
using Content.Shared.Power;
using Content.Shared.Storage.Components;
using Content.Shared.Storage.EntitySystems;
using Content.Shared.Verbs;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Timing;

namespace Content.Server._RedStar.Xenobiology;
public sealed partial class MutagenicInjectorSystem : EntitySystem
{
    [Dependency] private GeneticVialSystem _vials = default!;
    [Dependency] private ItemSlotsSystem _itemSlots = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private GenomeSystem _genomes = default!;
    [Dependency] private EntityStorageSystem _storage = default!;

    private bool IsValidRecipient(EntityUid recipient) =>
        HasComp<MobStateComponent>(recipient) && HasComp<BloodstreamComponent>(recipient) &&
        HasComp<GenomeComponent>(recipient);

    [SubscribeLocalEvent]
    private void OnInsertAttempt(Entity<CellMutagenicInjectorComponent> ent,
        ref EntityStorageInsertedIntoAttemptEvent args)
    {
        if (!IsValidRecipient(args.ItemToInsert))
            args.Cancelled = true;
    }

    [SubscribeLocalEvent]
    private void OnOpened(Entity<CellMutagenicInjectorComponent> ent, ref StorageAfterOpenEvent args)
    {
        CancelInjection(ent);
    }

    [SubscribeLocalEvent]
    private void OnPowerChanged(Entity<CellMutagenicInjectorComponent> ent, ref PowerChangedEvent args)
    {
        if (!args.Powered)
            CancelInjection(ent);
    }

    [SubscribeLocalEvent]
    private void OnGetAltVerbs(Entity<CellMutagenicInjectorComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract || args.Using is not null ||
            Comp<EntityStorageComponent>(ent).Open || ent.Comp.InjectionEndTime is not null)
            return;

        args.Verbs.Add(new AlternativeVerb
        {
            Text = Loc.GetString("mutagenic-injector-start"),
            Act = () => TryStartInjection(ent),
            Priority = 1
        });
    }

    [SubscribeLocalEvent]
    private void OnContainerRemoved(Entity<CellMutagenicInjectorComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        if (args.Container.ID == ent.Comp.VialSlot || args.Container.ID == SharedEntityStorageSystem.ContainerName)
            CancelInjection(ent);
    }

    [SubscribeLocalEvent]
    private void OnShutdown(Entity<CellMutagenicInjectorComponent> ent, ref ComponentShutdown args)
    {
        CancelInjection(ent);
    }

    private void TryStartInjection(Entity<CellMutagenicInjectorComponent> ent)
    {
        if (!TryGetReadyInjection(ent, out var subject, out var vial))
            return;

        ent.Comp.InjectionSubject = subject;
        ent.Comp.InjectionVial = vial.Owner;
        ent.Comp.InjectionVialRevision = vial.Comp.Revision;
        ent.Comp.InjectionEndTime = _timing.CurTime + ent.Comp.InjectionDelay;
        ent.Comp.AudioStream = _audio.PlayPvs(ent.Comp.ProcessSound, ent.Owner)?.Entity;
    }

    private bool TryGetReadyInjection(Entity<CellMutagenicInjectorComponent> ent, out EntityUid subject,
        out Entity<GeneticVialComponent> vial)
    {
        subject = default;
        vial = default;
        if (ent.Comp.InjectionEndTime is not null || !this.IsPowered(ent.Owner, EntityManager))
            return false;

        var storage = Comp<EntityStorageComponent>(ent);
        var vialUid = _itemSlots.GetItemOrNull(ent.Owner, ent.Comp.VialSlot);
        if (storage.Open || storage.Contents.ContainedEntities.Count != 1 ||
            !IsValidRecipient(storage.Contents.ContainedEntities.First()) ||
            vialUid is not { } inserted ||
            !TryComp<GeneticVialComponent>(inserted, out var vialComponent) ||
            vialComponent.Genes.Count == 0 ||
            _genomes.ValidateGenes(vialComponent.Genes) != GenomeOperationResult.Success)
            return false;

        subject = storage.Contents.ContainedEntities.First();
        vial = (inserted, vialComponent);
        return true;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<CellMutagenicInjectorComponent, EntityStorageComponent, MetaDataComponent>();
        while (query.MoveNext(out var uid, out var component, out var storage, out var metadata))
        {
            if (metadata.EntityPaused)
                continue;

            var machine = new Entity<CellMutagenicInjectorComponent>(uid, component);
            if (component.InjectionEndTime is not { } endTime)
                continue;

            if (!this.IsPowered(uid, EntityManager) ||
                storage.Open || storage.Contents.ContainedEntities.Count != 1 ||
                component.InjectionSubject != storage.Contents.ContainedEntities.First() ||
                component.InjectionVial != _itemSlots.GetItemOrNull(uid, component.VialSlot) ||
                component.InjectionVial is not { } vialUid ||
                !TryComp<GeneticVialComponent>(vialUid, out var vial) ||
                vial.Revision != component.InjectionVialRevision)
            {
                CancelInjection(machine);
                continue;
            }

            if (_timing.CurTime >= endTime)
                FinishInjection(machine);
        }
    }

    private void FinishInjection(Entity<CellMutagenicInjectorComponent> ent)
    {
        var subject = ent.Comp.InjectionSubject;
        var vial = ent.Comp.InjectionVial;
        if (subject is not { } mob || vial is not { } vialUid ||
            !TryComp<GeneticVialComponent>(vialUid, out var vialComponent) ||
            vialComponent.Revision != ent.Comp.InjectionVialRevision || vialComponent.Genes.Count == 0 ||
            !IsValidRecipient(mob) || _genomes.ApplyGenes(mob, _vials.GetGenes((vialUid, vialComponent)), out var changed) !=
            GenomeOperationResult.Success)
        {
            CancelInjection(ent);
            return;
        }

        _vials.Clear((vialUid, vialComponent));
        _storage.OpenStorage(ent.Owner);
        _popup.PopupEntity(Loc.GetString("mutagenic-injector-success", ("genes", changed)), ent, PopupType.Medium);
    }

    private void CancelInjection(Entity<CellMutagenicInjectorComponent> ent)
    {
        if (ent.Comp.InjectionEndTime is null)
            return;

        ent.Comp.InjectionEndTime = null;
        ent.Comp.InjectionSubject = null;
        ent.Comp.InjectionVial = null;
        ent.Comp.InjectionVialRevision = 0;
        ent.Comp.AudioStream = _audio.Stop(ent.Comp.AudioStream);
    }
}
