using Content.Server._RedStar.Xenobiology.Components;
using Content.Server.Administration.Logs;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Server.Power.EntitySystems;
using Content.Server._RedStar.Genetics.Systems;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Climbing.Systems;
using Content.Shared.Database;
using Content.Shared.FixedPoint;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Power;
using Content.Shared._RedStar.Genetics.Components;
using Content.Shared._RedStar.Genetics.Events;
using Content.Shared._RedStar.Genetics.Prototypes;
using Content.Shared._RedStar.Xenobiology.Components.Machines;
using Content.Shared._RedStar.Xenobiology.Visuals;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server._RedStar.Xenobiology.Systems;

/// <summary>
/// Mutates raw genes in a living subject using recipe reagents.
/// </summary>
public sealed partial class GrowingVatSystem : EntitySystem
{
    [Dependency] private PowerReceiverSystem _power = default!;
    [Dependency] private IAdminLogManager _logs = default!;
    [Dependency] private SharedContainerSystem _containers = default!;
    [Dependency] private ClimbSystem _climb = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedSolutionContainerSystem _solutions = default!;
    [Dependency] private ServerGenomeSystem _genomes = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private IPrototypeManager _prototypes = default!;

    [SubscribeLocalEvent]
    private void OnMapInit(Entity<GrowingVatComponent> ent, ref MapInitEvent args) =>
        _appearance.SetData(ent, GrowingVatVisuals.Processing, false);

    [SubscribeLocalEvent]
    private void OnStartup(Entity<GrowingVatComponent> ent, ref ComponentStartup args) => Reevaluate(ent);

    [SubscribeLocalEvent]
    private void OnPowerChanged(Entity<GrowingVatComponent> ent, ref PowerChangedEvent args) => Reevaluate(ent);

    [SubscribeLocalEvent]
    private void OnSolutionChanged(Entity<GrowingVatComponent> ent, ref SolutionChangedEvent args) => Reevaluate(ent);

    [SubscribeLocalEvent]
    private void OnGenomeChanged(Entity<GenomeComponent> ent, ref GenomeChangedEvent args) =>
        ReevaluateContainingVat(ent.Owner);

    [SubscribeLocalEvent]
    private void OnMobChanged(Entity<GenomeComponent> ent, ref MobStateChangedEvent args) =>
        ReevaluateContainingVat(ent.Owner);

    private void ReevaluateContainingVat(EntityUid subject)
    {
        if (_containers.TryGetContainingContainer(subject, out var slot) &&
            TryComp<GrowingVatComponent>(slot.Owner, out var vat) && slot.ID == vat.SubjectSlot)
            Reevaluate((slot.Owner, vat));
    }

    private void Reevaluate(Entity<GrowingVatComponent> vat)
    {
        vat.Comp.NeedsEvaluation = true;
        EnsureComp<ActiveGrowingVatComponent>(vat);
    }

    [SubscribeLocalEvent]
    private void OnInsertAttempt(Entity<GrowingVatComponent> ent, ref ContainerIsInsertingAttemptEvent args)
    {
        if (args.Container.ID == ent.Comp.SubjectSlot && !IsValidSubject(args.EntityUid))
            args.Cancel();
    }

    [SubscribeLocalEvent]
    private void OnInserted(Entity<GrowingVatComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        if (args.Container.ID == ent.Comp.SubjectSlot)
            Reevaluate(ent);
    }

    [SubscribeLocalEvent]
    private void OnContainerRemoved(Entity<GrowingVatComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        if (args.Container.ID == ent.Comp.SubjectSlot)
            Reevaluate(ent);
    }

    [SubscribeLocalEvent]
    private void OnShutdown(Entity<GrowingVatComponent> ent, ref ComponentShutdown args) => Cancel(ent);

    private bool IsValidSubject(Entity<GenomeComponent?> subject) =>
        Resolve(subject, ref subject.Comp, logMissing: false) &&
        HasComp<MobStateComponent>(subject) && !_mobState.IsDead(subject.Owner);

    private bool TryGetSubject(Entity<GrowingVatComponent> ent, out Entity<GenomeComponent> subject)
    {
        subject = default;
        if (!_containers.TryGetContainer(ent.Owner, ent.Comp.SubjectSlot, out var container) ||
            container.ContainedEntities.Count != 1)
            return false;

        var contained = container.ContainedEntities.First();
        if (!TryComp<GenomeComponent>(contained, out var genome) || !IsValidSubject((contained, genome)))
            return false;

        subject = (contained, genome);
        return true;
    }

    private bool TryGetRecipe(Entity<GrowingVatComponent> vat, Entity<GenomeComponent> subject,
        [NotNullWhen(true)] out GeneticMutationPrototype? recipe)
    {
        recipe = null;
        if (!_solutions.TryGetSolution(vat.Owner, vat.Comp.Solution, out _, out var mixture))
            return false;

        foreach (var candidate in _prototypes.EnumeratePrototypes<GeneticMutationPrototype>()
                     .OrderByDescending(prototype => prototype.Priority)
                     .ThenBy(prototype => prototype.ID, StringComparer.Ordinal))
        {
            if (!Eligible(subject, candidate, mixture))
                continue;

            recipe = candidate;
            return true;
        }

        return false;
    }

    private bool Eligible(Entity<GenomeComponent> subject, GeneticMutationPrototype recipe, Solution mixture) =>
        _genomes.CanTransformGenes(subject.AsNullable(), recipe.RequiredGenes, recipe.ResultGene) &&
        recipe.RequiredReagents.All(entry => mixture.GetTotalPrototypeQuantity(entry.Key.Id) >= entry.Value);

    private void SpendReagents(Entity<SolutionComponent> container, GeneticMutationPrototype recipe)
    {
        foreach (var (prototype, amount) in recipe.RequiredReagents)
        {
            var remaining = amount;
            foreach (var (reagent, quantity) in container.Comp.Solution.Contents.ToArray())
            {
                if (reagent.Prototype != prototype.Id)
                    continue;

                remaining -= _solutions.RemoveReagent(container, reagent, FixedPoint2.Min(remaining, quantity));
                if (remaining <= FixedPoint2.Zero)
                    break;
            }
        }
    }

    private bool TryGetActiveRecipe(Entity<GrowingVatComponent> vat, Entity<GenomeComponent> subject,
        [NotNullWhen(true)] out GeneticMutationPrototype? recipe, out Entity<SolutionComponent> container)
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

    private void Evaluate(Entity<GrowingVatComponent> vat)
    {
        if (_containers.TryGetContainer(vat.Owner, vat.Comp.SubjectSlot, out var container) &&
            container.ContainedEntities.FirstOrDefault() is { Valid: true } occupant && !IsValidSubject(occupant))
            Eject(vat, occupant);

        if (vat.Comp.Subject is { } subject && _power.IsPowered(vat.Owner) &&
            TryGetSubject(vat, out var contained) && subject == contained.Owner &&
            TryGetActiveRecipe(vat, contained, out _, out _))
            return;

        Cancel(vat);
        TryStart(vat);
    }

    private void TryStart(Entity<GrowingVatComponent> vat)
    {
        if (vat.Comp.Subject is not null || !_power.IsPowered(vat.Owner) ||
            !TryGetSubject(vat, out var subject) || !TryGetRecipe(vat, subject, out var recipe))
            return;

        vat.Comp.Subject = subject.Owner;
        vat.Comp.Recipe = recipe.ID;
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

        if (_containers.Remove(subject, container))
            _climb.ForciblySetClimbing(subject, vat.Owner);
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
        var query = EntityQueryEnumerator<ActiveGrowingVatComponent, GrowingVatComponent, MetaDataComponent>();
        while (query.MoveNext(out var uid, out _, out var component, out var metadata))
        {
            if (metadata.EntityPaused)
                continue;

            var vat = new Entity<GrowingVatComponent>(uid, component);
            if (component.NeedsEvaluation)
            {
                component.NeedsEvaluation = false;
                Evaluate(vat);
            }
            if (component.EndTime is not { } end)
            {
                RemComp<ActiveGrowingVatComponent>(uid);
                continue;
            }

            if (_timing.CurTime < end)
                continue;
            if (component.Subject is not { } subject || !_power.IsPowered(uid) ||
                !TryGetSubject(vat, out var contained) || subject != contained.Owner ||
                !TryGetActiveRecipe(vat, contained, out var recipe, out var solution))
            {
                Cancel(vat);
                continue;
            }

            Cancel(vat);
            SpendReagents(solution, recipe);
            if (_genomes.TryTransformGenes(contained.AsNullable(), recipe.RequiredGenes, recipe.ResultGene))
            {
                _logs.Add(LogType.Action, LogImpact.Medium,
                    $"{ToPrettyString(vat)} transformed the genes of {ToPrettyString(subject)} into {recipe.ResultGene}.");
                _popup.PopupEntity(Loc.GetString("growing-vat-success"), vat, subject, PopupType.Medium);
            }

            Eject(vat, subject);
        }
    }
}
