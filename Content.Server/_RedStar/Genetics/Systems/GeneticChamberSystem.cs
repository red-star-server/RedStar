using Content.Server._RedStar.Genetics.Components;
using Content.Server.Administration.Logs;
using Content.Server.Medical.Components;
using Content.Server.Power.EntitySystems;
using Content.Shared._RedStar.Genetics.Data;
using Content.Shared._RedStar.Genetics.Events;
using Content.Shared._RedStar.Genetics.Prototypes;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Database;
using Content.Shared.Power;
using Content.Shared.FixedPoint;
using Robust.Shared.Containers;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server._RedStar.Genetics.Systems;

/// <summary>
/// Consumes treatment reagents to activate, suppress or remove a selected acquired gene.
/// </summary>
public sealed partial class GeneticChamberSystem : EntitySystem
{
    [Dependency] private ServerGenomeSystem _genomes = default!;
    [Dependency] private SharedSolutionContainerSystem _solutions = default!;
    [Dependency] private ItemSlotsSystem _slots = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IAdminLogManager _logs = default!;
    [Dependency] private SharedAudioSystem _audio = default!;

    private TimeSpan _nextUpdate;

    /// <summary>
    /// Returns the occupant of the chamber's existing medical scanner container.
    /// </summary>
    public EntityUid? GetPatient(EntityUid chamber) =>
        TryComp<MedicalScannerComponent>(chamber, out var scanner) ? scanner.BodyContainer.ContainedEntity : null;

    /// <summary>
    /// Resolves the treatment solution in the inserted dispenser-compatible container.
    /// </summary>
    public bool TryGetTreatmentSolution(Entity<GeneticChamberComponent> chamber, out Entity<SolutionComponent> solution)
    {
        solution = default;
        var beaker = _slots.GetItemOrNull(chamber.Owner, chamber.Comp.BeakerSlot);
        if (beaker is null || !_solutions.TryGetFitsInDispenser(beaker.Value, out var entity, out _))
            return false;
        solution = entity.Value;
        return true;
    }

    /// <summary>
    /// Starts treatment of the expected occupant when the gene state and reagent dose permit it.
    /// </summary>
    public bool TryStartProcedure(Entity<GeneticChamberComponent> chamber, EntityUid console, EntityUid patient,
        ProtoId<GenePrototype> gene, GeneticProcedure procedure, EntityUid user)
    {
        if (chamber.Comp.Patient is not null || TerminatingOrDeleted(console) || TerminatingOrDeleted(patient) ||
            !this.IsPowered(chamber.Owner, EntityManager) || !Transform(chamber).Anchored || GetPatient(chamber) != patient ||
            !_genomes.TryGetGeneStatus(patient, gene, out var status) || !procedure.CanProcess(status) ||
            !chamber.Comp.Procedures.TryGetValue(procedure, out var settings) ||
            settings.Duration <= TimeSpan.Zero || settings.Dose <= FixedPoint2.Zero ||
            !TryGetTreatmentSolution(chamber, out var solution) ||
            solution.Comp.Solution.GetReagentQuantity(new ReagentId(settings.Reagent.Id, null)) < settings.Dose)
            return false;

        chamber.Comp.Console = console;
        chamber.Comp.Patient = patient;
        chamber.Comp.Beaker = _slots.GetItemOrNull(chamber.Owner, chamber.Comp.BeakerSlot);
        chamber.Comp.Gene = gene;
        chamber.Comp.InitialStatus = status;
        chamber.Comp.Operation = procedure;
        chamber.Comp.Consumed = FixedPoint2.Zero;
        chamber.Comp.NextSound = _timing.CurTime;
        chamber.Comp.Started = _timing.CurTime;
        chamber.Comp.Ends = _timing.CurTime + settings.Duration;
        _logs.Add(LogType.Action, LogImpact.Medium,
            $"{ToPrettyString(user)} started genetic {procedure} of {gene} on {ToPrettyString(patient)} using {ToPrettyString(chamber)}.");
        return true;
    }

    [SubscribeLocalEvent]
    private void OnRemoved(Entity<GeneticChamberComponent> chamber, ref EntRemovedFromContainerMessage args)
    {
        if (args.Entity == chamber.Comp.Patient || args.Entity == chamber.Comp.Beaker)
            CancelProcedure(chamber);
    }

    [SubscribeLocalEvent]
    private void OnShutdown(Entity<GeneticChamberComponent> chamber, ref ComponentShutdown args) => CancelProcedure(chamber);

    [SubscribeLocalEvent]
    private void OnChamberPower(Entity<GeneticChamberComponent> chamber, ref PowerChangedEvent args)
    {
        if (!args.Powered)
            CancelProcedure(chamber);
    }

    [SubscribeLocalEvent]
    private void OnChamberAnchor(Entity<GeneticChamberComponent> chamber, ref AnchorStateChangedEvent args)
    {
        if (!args.Anchored)
            CancelProcedure(chamber);
    }

    /// <summary>
    /// Ends processing without refunding consumed reagent and releases the controller's active reference.
    /// </summary>
    public void CancelProcedure(Entity<GeneticChamberComponent> chamber)
    {
        var console = chamber.Comp.Console;
        chamber.Comp.Patient = null;
        chamber.Comp.Console = null;
        chamber.Comp.Beaker = null;
        chamber.Comp.Consumed = FixedPoint2.Zero;
        if (console is not { } controller || Deleted(controller))
            return;

        var ended = new GeneticProcedureEndedEvent(chamber.Owner);
        RaiseLocalEvent(controller, ref ended);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        if (_timing.CurTime < _nextUpdate)
            return;
        _nextUpdate = _timing.CurTime + TimeSpan.FromSeconds(0.5);

        var chambers = EntityQueryEnumerator<GeneticChamberComponent, MetaDataComponent>();
        while (chambers.MoveNext(out var uid, out var chamber, out var metadata))
        {
            if (chamber.Patient is not null && !metadata.EntityPaused)
                Process((uid, chamber));
        }
    }

    private void Process(Entity<GeneticChamberComponent> chamber)
    {
        var component = chamber.Comp;
        if (component.Console is not { } console || TerminatingOrDeleted(console) ||
            GetPatient(chamber) != component.Patient || component.Patient is not { } patient || TerminatingOrDeleted(patient) ||
            _slots.GetItemOrNull(chamber.Owner, component.BeakerSlot) != component.Beaker ||
            !_genomes.TryGetGeneStatus(patient, component.Gene, out var status) || status != component.InitialStatus ||
            !component.Procedures.TryGetValue(component.Operation, out var settings) || !TryGetTreatmentSolution(chamber, out var solution))
        {
            CancelProcedure(chamber);
            return;
        }

        var progress = Math.Clamp((_timing.CurTime - component.Started).TotalSeconds / settings.Duration.TotalSeconds, 0, 1);
        var required = FixedPoint2.New(settings.Dose.Double() * progress) - component.Consumed;
        if (solution.Comp.Solution.GetReagentQuantity(new ReagentId(settings.Reagent.Id, null)) < required)
        {
            CancelProcedure(chamber);
            return;
        }
        component.Consumed += _solutions.RemoveReagent(solution, settings.Reagent.Id, required);
        if (required > FixedPoint2.Zero && _timing.CurTime >= component.NextSound)
        {
            _audio.PlayPvs(component.InjectionSound, chamber.Owner);
            component.NextSound = _timing.CurTime + component.InjectionSoundInterval;
        }
        if (_timing.CurTime < component.Ends)
            return;

        var changed = component.Operation switch
        {
            GeneticProcedure.Activate when status == GeneStatus.Suppressed => _genomes.TryResumeGene(patient, component.Gene),
            GeneticProcedure.Activate => _genomes.TryActivateGene(patient, component.Gene),
            GeneticProcedure.Suppress => _genomes.TrySuppressGene(patient, component.Gene),
            GeneticProcedure.Remove => _genomes.TryRemoveGene(patient, component.Gene),
            _ => false
        };
        if (changed)
        {
            _logs.Add(LogType.Action, LogImpact.Medium,
                $"{ToPrettyString(chamber)} completed genetic {component.Operation} of {component.Gene} on {ToPrettyString(patient)}.");
        }

        CancelProcedure(chamber);
    }
}
