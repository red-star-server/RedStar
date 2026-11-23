using Content.Server._RedStar.Xenobiology.Components;
using System.Linq;
using Content.Server.Administration.Logs;
using Content.Server.Power.EntitySystems;
using Content.Server.Storage.EntitySystems;
using Content.Server._RedStar.Genetics.Systems;
using Content.Shared.Body.Components;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Database;
using Content.Shared.Mobs.Components;
using Content.Shared.Popups;
using Content.Shared.Power;
using Content.Shared.Storage.Components;
using Content.Shared.Storage.EntitySystems;
using Content.Shared.Verbs;
using Content.Shared._RedStar.Genetics.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Timing;

namespace Content.Server._RedStar.Xenobiology.Systems;
public sealed partial class MutagenicInjectorSystem : EntitySystem
{
    [Dependency] private PowerReceiverSystem _power = default!;
    [Dependency] private IAdminLogManager _logs = default!;
    [Dependency] private GeneticVialSystem _vials = default!;
    [Dependency] private ItemSlotsSystem _itemSlots = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private ServerGenomeSystem _genomes = default!;
    [Dependency] private EntityStorageSystem _storage = default!;

    private bool IsValidRecipient(EntityUid recipient) =>
        HasComp<MobStateComponent>(recipient) && HasComp<BloodstreamComponent>(recipient) &&
        HasComp<GenomeComponent>(recipient);

    [SubscribeLocalEvent]
    private void OnInsertAttempt(Entity<MutagenicInjectorComponent> ent,
        ref EntityStorageInsertedIntoAttemptEvent args)
    {
        if (!IsValidRecipient(args.ItemToInsert))
            args.Cancelled = true;
    }

    [SubscribeLocalEvent]
    private void OnOpened(Entity<MutagenicInjectorComponent> ent, ref StorageAfterOpenEvent args)
    {
        CancelInjection(ent);
    }

    [SubscribeLocalEvent]
    private void OnPowerChanged(Entity<MutagenicInjectorComponent> ent, ref PowerChangedEvent args)
    {
        if (!args.Powered)
            CancelInjection(ent);
    }

    [SubscribeLocalEvent]
    private void OnGetAltVerbs(Entity<MutagenicInjectorComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        var user = args.User;
        if (ent.Comp.InjectionEndTime is not null)
        {
            args.Verbs.Add(new AlternativeVerb
            {
                Text = Loc.GetString("mutagenic-injector-cancel"),
                Act = () => CancelInjection(ent),
                Priority = 1
            });
            return;
        }

        args.Verbs.Add(new AlternativeVerb
        {
            Text = Loc.GetString("mutagenic-injector-start"),
            Act = () => TryStartInjection(ent, user),
            Priority = 1
        });
    }

    [SubscribeLocalEvent]
    private void OnContainerRemoved(Entity<MutagenicInjectorComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        if (args.Container.ID == ent.Comp.VialSlot || args.Container.ID == SharedEntityStorageSystem.ContainerName)
            CancelInjection(ent);
    }

    [SubscribeLocalEvent]
    private void OnShutdown(Entity<MutagenicInjectorComponent> ent, ref ComponentShutdown args)
    {
        CancelInjection(ent);
    }

    private void TryStartInjection(Entity<MutagenicInjectorComponent> ent, EntityUid user)
    {
        if (Comp<EntityStorageComponent>(ent).Open)
        {
            _popup.PopupEntity(Loc.GetString("mutagenic-injector-open"), ent, user, PopupType.MediumCaution);
            return;
        }

        if (!TryGetReadyInjection(ent, user, out var subject, out var vial))
            return;

        ent.Comp.InjectionSubject = subject;
        ent.Comp.InjectionVial = vial.Owner;
        ent.Comp.InjectionVialRevision = vial.Comp.Revision;
        ent.Comp.InjectionEndTime = _timing.CurTime + ent.Comp.InjectionDelay;
        EnsureComp<ActiveMutagenicInjectorComponent>(ent);
        _logs.Add(LogType.Action, LogImpact.Medium,
            $"{ToPrettyString(user)} started a mutagenic injection of {string.Join(", ", vial.Comp.Genes)} into {ToPrettyString(subject)} using {ToPrettyString(ent)}.");
        ent.Comp.AudioStream = _audio.PlayPvs(ent.Comp.ProcessSound, ent.Owner)?.Entity;
    }

    private bool TryGetReadyInjection(Entity<MutagenicInjectorComponent> ent, EntityUid user, out EntityUid subject,
        out Entity<GeneticVialComponent> vial)
    {
        subject = default;
        vial = default;
        if (ent.Comp.InjectionEndTime is not null)
            return false;

        if (!TryGetContents((ent.Owner, ent.Comp, null), out subject, out vial) ||
            vial.Comp.Genes.Count == 0 || !IsValidRecipient(subject) ||
            !TryComp<GenomeComponent>(subject, out var genome))
            return false;

        var recipient = new Entity<GenomeComponent>(subject, genome);
        if (vial.Comp.Genes.All(id => _genomes.HasGene(recipient.AsNullable(), id)))
        {
            _popup.PopupEntity(Loc.GetString("mutagenic-injector-no-new-genes"), ent, user, PopupType.MediumCaution);
            return false;
        }

        if (vial.Comp.Genes.Any(id => !_genomes.IsGeneCompatible(recipient.Owner, id)))
        {
            _popup.PopupEntity(Loc.GetString("mutagenic-injector-incompatible"), ent, user, PopupType.MediumCaution);
            return false;
        }

        return _genomes.CanApplyGenes(recipient.AsNullable(), vial.Comp.Genes);
    }

    private bool TryGetContents(Entity<MutagenicInjectorComponent, EntityStorageComponent?> ent,
        out EntityUid subject, out Entity<GeneticVialComponent> vial)
    {
        subject = default;
        vial = default;
        if (!Resolve(ent, ref ent.Comp2) || !_power.IsPowered(ent.Owner) ||
            ent.Comp2.Open || ent.Comp2.Contents.ContainedEntities.Count != 1 ||
            _itemSlots.GetItemOrNull(ent.Owner, ent.Comp1.VialSlot) is not { } vialUid ||
            !TryComp<GeneticVialComponent>(vialUid, out var contents))
            return false;

        subject = ent.Comp2.Contents.ContainedEntities[0];
        vial = (vialUid, contents);
        return true;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<ActiveMutagenicInjectorComponent, MutagenicInjectorComponent, EntityStorageComponent, MetaDataComponent>();
        while (query.MoveNext(out var uid, out _, out var component, out var storage, out var metadata))
        {
            if (metadata.EntityPaused)
                continue;

            var machine = new Entity<MutagenicInjectorComponent>(uid, component);
            if (component.InjectionEndTime is not { } endTime)
                continue;

            if (!TryGetContents((uid, component, storage), out var subject, out var vial) ||
                component.InjectionSubject != subject || component.InjectionVial != vial.Owner ||
                vial.Comp.Revision != component.InjectionVialRevision)
            {
                CancelInjection(machine);
                continue;
            }

            if (_timing.CurTime >= endTime)
                FinishInjection(machine, subject, vial);
        }
    }

    private void FinishInjection(Entity<MutagenicInjectorComponent> ent, EntityUid subject,
        Entity<GeneticVialComponent> vial)
    {
        if (vial.Comp.Genes.Count == 0 || !IsValidRecipient(subject) ||
            !_genomes.TryApplyGenes(subject, vial.Comp.Genes, out var changed) || changed == 0)
        {
            CancelInjection(ent);
            return;
        }

        _logs.Add(LogType.Action, LogImpact.Medium,
            $"{ToPrettyString(ent)} injected {string.Join(", ", vial.Comp.Genes)} into {ToPrettyString(subject)}.");
        _vials.Clear(vial);
        CancelInjection(ent);
        _storage.OpenStorage(ent.Owner);
    }

    private void CancelInjection(Entity<MutagenicInjectorComponent> ent)
    {
        if (ent.Comp.InjectionEndTime is null)
            return;

        ent.Comp.InjectionEndTime = null;
        ent.Comp.InjectionSubject = null;
        ent.Comp.InjectionVial = null;
        ent.Comp.InjectionVialRevision = 0;
        RemComp<ActiveMutagenicInjectorComponent>(ent);
        ent.Comp.AudioStream = _audio.Stop(ent.Comp.AudioStream);
    }
}
