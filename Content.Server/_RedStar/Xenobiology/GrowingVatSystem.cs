using System.Linq;
using Content.Server._RedStar.Genetics;
using Content.Server.Power.EntitySystems;
using Content.Shared._RedStar.Genetics.Prototypes;
using Content.Shared._RedStar.Xenobiology.Components.Machines;
using Content.Shared._RedStar.Xenobiology.Visuals;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Climbing.Systems;
using Content.Shared.DragDrop;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Power;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._RedStar.Xenobiology;

/// <summary>
/// Mutates raw genes in a living subject using recipe reagents.
/// </summary>
public sealed partial class GrowingVatSystem : EntitySystem
{
    [Dependency] private SharedContainerSystem _containers = default!;
    [Dependency] private ClimbSystem _climb = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedSolutionContainerSystem _solutions = default!;
    [Dependency] private GenomeSystem _genomes = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private IPrototypeManager _prototypes = default!;

    [SubscribeLocalEvent]
    private void OnMapInit(Entity<GrowingVatComponent> ent, ref MapInitEvent args) =>
        _appearance.SetData(ent, GrowingVatVisuals.Processing, false);

    [SubscribeLocalEvent]
    private void OnPowerChanged(Entity<GrowingVatComponent> ent, ref PowerChangedEvent args)
    {
        Cancel(ent);
        if (args.Powered)
            TryStart(ent);
    }

    [SubscribeLocalEvent]
    private void OnSolutionChanged(Entity<GrowingVatComponent> ent, ref SolutionChangedEvent args)
    {
        if (ent.Comp.Completing)
            return;
        if (ent.Comp.Subject is { } subject && TryGetActiveRecipe(ent, subject, out _, out _))
            return;
        Cancel(ent);
        TryStart(ent);
    }

    [SubscribeLocalEvent]
    private void OnGenomeChanged(Entity<GenomeComponent> ent, ref GenomeChangedEvent args) => RefreshSubject(ent.Owner);

    [SubscribeLocalEvent]
    private void OnMobChanged(Entity<GenomeComponent> ent, ref MobStateChangedEvent args) => RefreshSubject(ent.Owner);

    private void RefreshSubject(EntityUid subject)
    {
        if (!_containers.TryGetContainingContainer(subject, out var slot) ||
            !TryComp<GrowingVatComponent>(slot.Owner, out var component) || slot.ID != component.SubjectSlot ||
            component.Completing)
            return;
        var vat = new Entity<GrowingVatComponent>(slot.Owner, component);
        Cancel(vat);
        if (!IsValidSubject(subject))
            Eject(vat, subject);
        else
            TryStart(vat);
    }

    [SubscribeLocalEvent]
    private void OnCanDrop(Entity<GrowingVatComponent> ent, ref CanDropTargetEvent args)
    {
        args.Handled = true;
        args.CanDrop = IsValidSubject(args.Dragged) &&
                       _containers.TryGetContainer(ent.Owner, ent.Comp.SubjectSlot, out var container) &&
                       _containers.CanInsert(args.Dragged, container);
    }

    [SubscribeLocalEvent]
    private void OnDragDrop(Entity<GrowingVatComponent> ent, ref DragDropTargetEvent args)
    {
        if (args.Handled || !IsValidSubject(args.Dragged) ||
            !_containers.TryGetContainer(ent.Owner, ent.Comp.SubjectSlot, out var container) ||
            !_containers.CanInsert(args.Dragged, container))
            return;

        args.Handled = _containers.Insert(args.Dragged, container);
    }

    [SubscribeLocalEvent]
    private void OnInserted(Entity<GrowingVatComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        if (args.Container.ID == ent.Comp.SubjectSlot)
            TryStart(ent);
    }

    [SubscribeLocalEvent]
    private void OnContainerRemoved(Entity<GrowingVatComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        if (args.Container.ID == ent.Comp.SubjectSlot)
            Cancel(ent);
    }

    [SubscribeLocalEvent]
    private void OnShutdown(Entity<GrowingVatComponent> ent, ref ComponentShutdown args) => Cancel(ent);

    private bool IsValidSubject(EntityUid uid) =>
        HasComp<GenomeComponent>(uid) && HasComp<MobStateComponent>(uid) && !_mobState.IsDead(uid);

    private bool TryGetSubject(Entity<GrowingVatComponent> ent, out EntityUid subject)
    {
        subject = default;
        if (!_containers.TryGetContainer(ent.Owner, ent.Comp.SubjectSlot, out var container) ||
            container.ContainedEntities.Count != 1)
            return false;

        var contained = container.ContainedEntities.First();
        if (!IsValidSubject(contained))
            return false;

        subject = contained;
        return true;
    }

    private bool TryGetRecipe(Entity<GrowingVatComponent> vat, EntityUid subject,
        out GeneticMutationPrototype? recipe)
    {
        recipe = null;
        if (!_solutions.TryGetSolution(vat.Owner, vat.Comp.Solution, out _, out var mixture))
            return false;

        var recipes = _prototypes.EnumeratePrototypes<GeneticMutationPrototype>()
            .OrderByDescending(r => r.RequiredGenes.Count)
            .ThenBy(r => r.ID, StringComparer.Ordinal);
        foreach (var candidate in recipes)
        {
            if (!Eligible(subject, candidate, mixture))
                continue;

            recipe = candidate;
            return true;
        }

        return false;
    }

    private bool Eligible(EntityUid subject, GeneticMutationPrototype recipe, Solution mixture) =>
        _genomes.CanTransformGenes(subject, recipe.RequiredGenes, recipe.ResultGene) &&
        recipe.RequiredGenes.All(id => _genomes.IsRaw(subject, id)) &&
        recipe.RequiredReagents.All(entry => mixture.GetTotalPrototypeQuantity(entry.Key.Id) >= entry.Value);

    private void SpendReagents(Entity<SolutionComponent> container, GeneticMutationPrototype recipe)
    {
        foreach (var (prototype, amount) in recipe.RequiredReagents)
        {
            container.Comp.Solution.RemoveReagent(prototype.Id, amount, ignoreReagentData: true);
        }

        _solutions.UpdateChemicals(container);
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

    private void TryStart(Entity<GrowingVatComponent> vat)
    {
        if (vat.Comp.Subject is not null || !this.IsPowered(vat.Owner, EntityManager) ||
            !TryGetSubject(vat, out var subject) || !TryGetRecipe(vat, subject, out var recipe))
            return;

        vat.Comp.Subject = subject;
        vat.Comp.Recipe = recipe!.ID;
        vat.Comp.EndTime = _timing.CurTime + recipe.Duration;
        UpdateVisual(vat, true);
    }

    private void Cancel(Entity<GrowingVatComponent> vat)
    {
        vat.Comp.Subject = null;
        vat.Comp.Recipe = null;
        vat.Comp.EndTime = null;
        UpdateVisual(vat, false);
    }

    private void Eject(Entity<GrowingVatComponent> vat, EntityUid subject)
    {
        if (!_containers.TryGetContainer(vat.Owner, vat.Comp.SubjectSlot, out var container) ||
            !container.Contains(subject))
            return;

        _climb.ForciblySetClimbing(subject, vat.Owner);
        _containers.Remove(subject, container);
    }

    private void UpdateVisual(Entity<GrowingVatComponent> vat, bool processing)
    {
        if (vat.Comp.Processing == processing)
            return;

        vat.Comp.Processing = processing;
        _appearance.SetData(vat, GrowingVatVisuals.Processing, processing);
        vat.Comp.AudioStream = processing
            ? _audio.PlayPvs(vat.Comp.ProcessSound, vat.Owner)?.Entity
            : _audio.Stop(vat.Comp.AudioStream);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var query = EntityQueryEnumerator<GrowingVatComponent, MetaDataComponent>();
        while (query.MoveNext(out var uid, out var component, out var metadata))
        {
            if (metadata.EntityPaused || component.EndTime is not { } end || _timing.CurTime < end)
                continue;

            var vat = new Entity<GrowingVatComponent>(uid, component);
            if (component.Subject is not { } subject || !this.IsPowered(uid, EntityManager) ||
                !TryGetSubject(vat, out var contained) || subject != contained ||
                !TryGetActiveRecipe(vat, subject, out var recipe, out var solution))
            {
                Cancel(vat);
                continue;
            }

            // Chemical and genome events caused by this transaction must not start another cycle.
            component.Completing = true;
            Cancel(vat);
            SpendReagents(solution, recipe!);
            var success = _random.Prob(recipe!.Chance) &&
                _genomes.TryTransformGenes(subject, recipe.RequiredGenes, recipe.ResultGene);
            _popup.PopupEntity(Loc.GetString(success ? "growing-vat-success" : "growing-vat-failure"),
                vat, success ? PopupType.Medium : PopupType.MediumCaution);
            Eject(vat, subject);
            component.Completing = false;
        }
    }
}
