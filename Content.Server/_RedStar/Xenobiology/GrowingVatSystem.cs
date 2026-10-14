using System.Linq;
using Content.Server._RedStar.Genetics;
using Content.Server.Power.EntitySystems;
using Content.Server.Storage.EntitySystems;
using Content.Shared._RedStar.Genetics;
using Content.Shared._RedStar.Xenobiology.Components.Machines;
using Content.Shared._RedStar.Xenobiology.Visuals;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.DragDrop;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Power;
using Content.Shared.Storage.Components;
using Content.Shared.Storage.EntitySystems;
using Content.Shared.Verbs;
using Robust.Shared.Containers;
using Robust.Shared.Random;

namespace Content.Server._RedStar.Xenobiology;

/// <summary>
/// Cultivates an existing organism and attempts one mutation per completed cycle.
/// </summary>
public sealed partial class GrowingVatSystem : EntitySystem
{
    [Dependency] private EntityStorageSystem _storage = default!;
    [Dependency] private SharedSolutionContainerSystem _solutions = default!;
    [Dependency] private GenomeSystem _genomes = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private IRobustRandom _random = default!;

    private const string Nutriment = "Nutriment";
    private const string Saline = "Saline";

    [SubscribeLocalEvent]
    private void OnMapInit(Entity<GrowingVatComponent> ent, ref MapInitEvent args) => UpdateVisual(ent, false);

    [SubscribeLocalEvent]
    private void OnPowerChanged(Entity<GrowingVatComponent> ent, ref PowerChangedEvent args) =>
        UpdateVisual(ent, false);

    [SubscribeLocalEvent]
    private void OnInsertAttempt(Entity<GrowingVatComponent> ent, ref EntityStorageInsertedIntoAttemptEvent args)
    {
        if (!IsValidSubject(args.ItemToInsert))
            args.Cancelled = true;
    }

    [SubscribeLocalEvent]
    private void OnCanDrop(Entity<GrowingVatComponent> ent, ref CanDropTargetEvent args)
    {
        args.Handled = true;
        args.CanDrop = TryComp<EntityStorageComponent>(ent, out var storage) && !storage.Open &&
                       IsValidSubject(args.Dragged) && _storage.CanInsert(args.Dragged, ent, storage);
    }

    [SubscribeLocalEvent]
    private void OnDragDrop(Entity<GrowingVatComponent> ent, ref DragDropTargetEvent args)
    {
        if (args.Handled || !TryComp<EntityStorageComponent>(ent, out var storage) || storage.Open ||
            !IsValidSubject(args.Dragged) || !_storage.CanInsert(args.Dragged, ent, storage))
            return;

        args.Handled = _storage.Insert(args.Dragged, ent, storage);
    }

    [SubscribeLocalEvent]
    private void OnGetAltVerbs(Entity<GrowingVatComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract || ent.Comp.Subject is not null ||
            !TryGetSubject(ent, out _) || !this.IsPowered(ent.Owner, EntityManager) ||
            !HasMedium(ent, out _, true))
            return;

        args.Verbs.Add(new AlternativeVerb
        {
            Text = Loc.GetString("growing-vat-start"),
            Act = () => TryStart(ent),
            Priority = 1
        });
    }

    [SubscribeLocalEvent]
    private void OnOpened(Entity<GrowingVatComponent> ent, ref StorageAfterOpenEvent args) => Cancel(ent);

    [SubscribeLocalEvent]
    private void OnContainerRemoved(Entity<GrowingVatComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        if (args.Container.ID == SharedEntityStorageSystem.ContainerName)
            Cancel(ent);
    }

    [SubscribeLocalEvent]
    private void OnShutdown(Entity<GrowingVatComponent> ent, ref ComponentShutdown args) => Cancel(ent);

    private bool IsValidSubject(EntityUid uid) =>
        HasComp<GenomeComponent>(uid) && HasComp<MobStateComponent>(uid) && !_mobState.IsDead(uid);

    private bool TryGetSubject(Entity<GrowingVatComponent> ent, out EntityUid subject)
    {
        subject = default;
        if (!TryComp<EntityStorageComponent>(ent, out var storage) || storage.Open ||
            storage.Contents.ContainedEntities.Count != 1)
            return false;

        var contained = storage.Contents.ContainedEntities.First();
        if (!IsValidSubject(contained))
            return false;

        subject = contained;
        return true;
    }

    private bool HasMedium(Entity<GrowingVatComponent> ent, out Entity<SolutionComponent> solution,
        bool starting = false)
    {
        solution = default;
        if (!_solutions.TryGetSolution(ent.Owner, ent.Comp.Solution, out var contained, out var mixture))
            return false;

        solution = contained.Value;
        return mixture.GetTotalPrototypeQuantity(Nutriment) >=
               (starting ? ent.Comp.MinimumNutriment : ent.Comp.NutrimentPerSecond) &&
               mixture.GetTotalPrototypeQuantity(Saline) >=
               (starting ? ent.Comp.MinimumSaline : ent.Comp.SalinePerSecond);
    }

    private void TryStart(Entity<GrowingVatComponent> ent)
    {
        if (ent.Comp.Subject is not null || !this.IsPowered(ent.Owner, EntityManager) ||
            !TryGetSubject(ent, out var subject) || !HasMedium(ent, out _, true))
            return;

        ent.Comp.Subject = subject;
        ent.Comp.Progress = TimeSpan.Zero;
        ent.Comp.TickAccumulator = 0f;
        UpdateVisual(ent, true);
    }

    private void Cancel(Entity<GrowingVatComponent> ent)
    {
        ent.Comp.Subject = null;
        ent.Comp.Progress = TimeSpan.Zero;
        ent.Comp.TickAccumulator = 0f;
        UpdateVisual(ent, false);
    }

    private void UpdateVisual(Entity<GrowingVatComponent> ent, bool processing) =>
        _appearance.SetData(ent, GrowingVatVisuals.Processing, processing);

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<GrowingVatComponent, MetaDataComponent>();
        while (query.MoveNext(out var uid, out var component, out var metadata))
        {
            if (metadata.EntityPaused || component.Subject is not { } subject)
                continue;

            var vat = new Entity<GrowingVatComponent>(uid, component);
            if (!TryGetSubject(vat, out var contained) || contained != subject)
            {
                Cancel(vat);
                continue;
            }

            if (!this.IsPowered(uid, EntityManager) || !HasMedium(vat, out var solution))
            {
                UpdateVisual(vat, false);
                continue;
            }

            UpdateVisual(vat, true);
            component.TickAccumulator += frameTime;
            while (component.TickAccumulator >= 1f)
            {
                if (!HasMedium(vat, out solution))
                {
                    UpdateVisual(vat, false);
                    break;
                }

                _solutions.RemoveReagent(solution, Nutriment, component.NutrimentPerSecond);
                _solutions.RemoveReagent(solution, Saline, component.SalinePerSecond);
                component.TickAccumulator -= 1f;
                component.Progress += TimeSpan.FromSeconds(1);
                if (component.Progress < component.CycleDuration)
                    continue;

                Complete(vat, subject);
                break;
            }
        }
    }

    private void Complete(Entity<GrowingVatComponent> vat, EntityUid subject)
    {
        Cancel(vat);
        if (!_genomes.HasGene(subject, "NightVision") || !_genomes.HasGene(subject, "Regeneration") ||
            _genomes.HasGene(subject, "NoSlip") || !float.IsFinite(vat.Comp.MutationChance) ||
            !_random.Prob(Math.Clamp(vat.Comp.MutationChance, 0f, 1f)))
            return;

        _genomes.SetGene(subject, "NoSlip", new GeneState());
    }
}
