using Content.Server._RedStar.Genetics;
using Content.Shared._RedStar.Genetics;
using Content.Server.Power.EntitySystems;
using Content.Shared._RedStar.Xenobiology.Components.Machines;
using Content.Shared._RedStar.Xenobiology.Systems.Machines;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Power;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server._RedStar.Xenobiology;
public sealed partial class MutagenicInjectorSystem : SharedMutagenicInjectorSystem
{
    [Dependency] private GeneticVialSystem _vials = default!;
    [Dependency] private ItemSlotsSystem _itemSlots = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private GenomeSystem _genomes = default!;

    protected override bool IsValidRecipient(EntityUid recipient) =>
        base.IsValidRecipient(recipient) && HasComp<GenomeComponent>(recipient);

    [SubscribeLocalEvent]
    private void OnActivateInWorld(Entity<CellMutagenicInjectorComponent> ent, ref ActivateInWorldEvent args)
    {
        if (args.Handled)
            return;

        TryStartInjection(ent);
        args.Handled = true;
    }

    [SubscribeLocalEvent]
    private void OnPowerChanged(Entity<CellMutagenicInjectorComponent> ent, ref PowerChangedEvent args)
    {
        if (!args.Powered)
            CancelInjection(ent);
    }

    [SubscribeLocalEvent]
    private void OnContainerRemoved(Entity<CellMutagenicInjectorComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        if (args.Container.ID == ent.Comp.VialSlot || args.Container.ID == CellMutagenicInjectorComponent.BodyContainerId)
            CancelInjection(ent);
    }

    [SubscribeLocalEvent]
    private void OnShutdown(Entity<CellMutagenicInjectorComponent> ent, ref ComponentShutdown args)
    {
        CancelInjection(ent);
    }

    private void TryStartInjection(Entity<CellMutagenicInjectorComponent> ent)
    {
        if (ent.Comp.InjectionEndTime is not null || !this.IsPowered(ent.Owner, EntityManager) ||
            !ValidateInjection(ent))
            return;

        var vial = _itemSlots.GetItemOrNull(ent.Owner, ent.Comp.VialSlot)!.Value;
        var vialComponent = Comp<GeneticVialComponent>(vial);

        ent.Comp.InjectionSubject = ent.Comp.BodyContainer.ContainedEntity;
        ent.Comp.InjectionVial = vial;
        ent.Comp.InjectionVialRevision = vialComponent.Revision;
        ent.Comp.InjectionEndTime = _timing.CurTime + ent.Comp.InjectionDelay;
        ent.Comp.AudioStream = _audio.PlayPvs(ent.Comp.ProcessSound, ent.Owner)?.Entity;
    }

    private bool ValidateInjection(Entity<CellMutagenicInjectorComponent> ent)
    {
        var vial = _itemSlots.GetItemOrNull(ent.Owner, ent.Comp.VialSlot);
        if (vial is null)
        {
            _popup.PopupEntity(Loc.GetString("mutagenic-injector-no-vial"), ent, PopupType.MediumCaution);
            return false;
        }

        if (!TryComp<GeneticVialComponent>(vial.Value, out var vialComponent) || vialComponent.Genes.Count == 0)
        {
            _popup.PopupEntity(Loc.GetString("mutagenic-injector-no-genes"), ent, PopupType.MediumCaution);
            return false;
        }

        if (_genomes.ValidateGenes(vialComponent.Genes) != GenomeOperationResult.Success)
        {
            _popup.PopupEntity(Loc.GetString("mutagenic-injector-invalid-vial"), ent, PopupType.MediumCaution);
            return false;
        }

        if (ent.Comp.BodyContainer.ContainedEntity is { } recipient && IsValidRecipient(recipient))
            return true;

        _popup.PopupEntity(Loc.GetString("mutagenic-injector-no-target"), ent, PopupType.MediumCaution);
        return false;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<CellMutagenicInjectorComponent, MetaDataComponent>();
        while (query.MoveNext(out var uid, out var component, out var metadata))
        {
            if (metadata.EntityPaused || component.InjectionEndTime is not { } endTime)
                continue;

            var machine = new Entity<CellMutagenicInjectorComponent>(uid, component);
            if (!this.IsPowered(uid, EntityManager) ||
                component.InjectionSubject != component.BodyContainer.ContainedEntity ||
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
            !IsValidRecipient(mob))
        {
            CancelInjection(ent);
            return;
        }

        if (_genomes.ApplyGenes(mob, _vials.GetGenes((vialUid, vialComponent)), out var changed) !=
            GenomeOperationResult.Success)
        {
            CancelInjection(ent);
            return;
        }

        _vials.Clear((vialUid, vialComponent));
        EjectBody(ent);
        _popup.PopupEntity(Loc.GetString("mutagenic-injector-success", ("genes", changed)), ent, PopupType.Medium);
    }

    protected override void CancelInjection(Entity<CellMutagenicInjectorComponent> ent)
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
