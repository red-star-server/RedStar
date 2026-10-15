using System.Linq;
using Content.Server._RedStar.Genetics;
using Content.Server.Power.EntitySystems;
using Content.Server.Storage.EntitySystems;
using Content.Shared._RedStar.Genetics;
using Content.Shared._RedStar.Genetics.Prototypes;
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
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._RedStar.Xenobiology;

/// <summary>
/// Incubates an existing organism. Mutation recipes own the required genes,
/// total reagent cost and result; the vat only operates the procedure.
/// </summary>
public sealed partial class GrowingVatSystem : EntitySystem
{
    [Dependency] private EntityStorageSystem _storage = default!;
    [Dependency] private SharedSolutionContainerSystem _solutions = default!;
    [Dependency] private GenomeSystem _genomes = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private IPrototypeManager _prototypes = default!;

    [SubscribeLocalEvent]
    private void OnMapInit(Entity<GrowingVatComponent> ent, ref MapInitEvent args) =>
        _appearance.SetData(ent, GrowingVatVisuals.Processing, false);

    [SubscribeLocalEvent]
    private void OnPowerChanged(Entity<GrowingVatComponent> ent, ref PowerChangedEvent args) =>
        UpdateVisual(ent, CanProcess(ent));

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
            !TryGetSubject(ent, out var subject) || !this.IsPowered(ent.Owner, EntityManager) ||
            !TryGetRecipe(ent, subject, out _))
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

    /// <summary>
    /// The first eligible recipe in stable ID order wins. Reagents are a total
    /// cost per completed cycle, not a per-second consumption rate.
    /// </summary>
    private bool TryGetRecipe(Entity<GrowingVatComponent> vat, EntityUid subject,
        out GeneticMutationPrototype? recipe)
    {
        recipe = null;
        if (!_solutions.TryGetSolution(vat.Owner, vat.Comp.Solution, out _, out var mixture))
            return false;

        foreach (var candidate in _prototypes.EnumeratePrototypes<GeneticMutationPrototype>().OrderBy(r => r.ID))
        {
            if (!Eligible(subject, candidate, mixture))
                continue;

            recipe = candidate;
            return true;
        }

        return false;
    }

    private bool Eligible(EntityUid subject, GeneticMutationPrototype recipe, Solution mixture) =>
        !_genomes.HasGene(subject, recipe.ResultGene) &&
        recipe.RequiredGenes.All(id => _genomes.HasGene(subject, id)) &&
        recipe.RequiredReagents.All(entry => entry.Value > 0 &&
            mixture.GetTotalPrototypeQuantity(entry.Key.Id) >= entry.Value);

    private bool TrySpendReagents(Entity<SolutionComponent> container, GeneticMutationPrototype recipe)
    {
        var source = container.Comp.Solution;
        var remaining = source.Clone();
        foreach (var (prototype, amount) in recipe.RequiredReagents)
        {
            var needed = amount;
            foreach (var (reagent, quantity) in source.Contents)
            {
                if (reagent.Prototype != prototype.Id)
                    continue;

                var take = needed < quantity ? needed : quantity;
                needed -= remaining.RemoveReagent(reagent, take);
                if (needed <= 0)
                    break;
            }

            if (needed > 0)
                return false;
        }

        container.Comp.Solution = remaining;
        _solutions.UpdateChemicals(container);
        return true;
    }

    private bool TryGetActiveRecipe(Entity<GrowingVatComponent> vat, EntityUid subject,
        out GeneticMutationPrototype? recipe, out Entity<SolutionComponent> container)
    {
        recipe = null;
        container = default;
        if (vat.Comp.Recipe is not { } id || !_prototypes.TryIndex(id, out recipe) ||
            !_solutions.TryGetSolution(vat.Owner, vat.Comp.Solution, out var solution, out var mixture) ||
            !Eligible(subject, recipe, mixture))
            return false;

        container = solution.Value;
        return true;
    }

    private bool CanProcess(Entity<GrowingVatComponent> vat) =>
        vat.Comp.Subject is { } subject &&
        this.IsPowered(vat.Owner, EntityManager) &&
        TryGetSubject(vat, out var contained) && contained == subject &&
        TryGetActiveRecipe(vat, subject, out _, out _);

    private void TryStart(Entity<GrowingVatComponent> vat)
    {
        if (vat.Comp.Subject is not null || !this.IsPowered(vat.Owner, EntityManager) ||
            !TryGetSubject(vat, out var subject) || !TryGetRecipe(vat, subject, out var recipe))
            return;

        vat.Comp.Subject = subject;
        vat.Comp.Recipe = recipe!.ID;
        vat.Comp.Progress = TimeSpan.Zero;
        vat.Comp.TickAccumulator = 0f;
        UpdateVisual(vat, true);
    }

    private void Cancel(Entity<GrowingVatComponent> vat)
    {
        vat.Comp.Subject = null;
        vat.Comp.Recipe = null;
        vat.Comp.Progress = TimeSpan.Zero;
        vat.Comp.TickAccumulator = 0f;
        UpdateVisual(vat, false);
    }

    private void UpdateVisual(Entity<GrowingVatComponent> vat, bool processing)
    {
        if (vat.Comp.Processing == processing)
            return;

        vat.Comp.Processing = processing;
        _appearance.SetData(vat, GrowingVatVisuals.Processing, processing);
    }

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

            // Recipes and chemistry are checked once per second, not every server tick.
            component.TickAccumulator += frameTime;
            if (component.TickAccumulator < 1f)
                continue;
            var elapsed = component.TickAccumulator;
            component.TickAccumulator = 0f;

            if (!this.IsPowered(uid, EntityManager) ||
                !TryGetActiveRecipe(vat, subject, out var recipe, out var solution))
            {
                UpdateVisual(vat, false);
                continue;
            }

            UpdateVisual(vat, true);
            component.Progress += TimeSpan.FromSeconds(elapsed);
            if (component.Progress < recipe!.Duration)
                continue;

            if (!TrySpendReagents(solution, recipe))
            {
                UpdateVisual(vat, false);
                continue;
            }

            Cancel(vat);
            if (float.IsFinite(recipe.Chance) &&
                _random.Prob(Math.Clamp(recipe.Chance, 0f, 1f)))
                _genomes.SetGene(subject, recipe.ResultGene, new GeneState());
        }
    }
}
