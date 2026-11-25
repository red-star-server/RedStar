using Content.Shared._RedStar.Genetics.Components;
using System.Linq;
using System.Diagnostics.CodeAnalysis;
using Content.Server._RedStar.Genetics.Components;
using Content.Server._RedStar.Genetics.Data;
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
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server._RedStar.Genetics.Systems;

/// <summary>
/// Consumes treatment reagents to activate, suppress or remove a selected acquired gene.
/// </summary>
public sealed partial class GeneticChamberSystem : EntitySystem
{
    [Dependency] private PowerReceiverSystem _power = default!;
    [Dependency] private GeneticVialSystem _vials = default!;
    [Dependency] private ServerGenomeSystem _genomes = default!;
    [Dependency] private SharedSolutionContainerSystem _solutions = default!;
    [Dependency] private ItemSlotsSystem _slots = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IAdminLogManager _logs = default!;

    private TimeSpan _nextUpdate;

    /// <summary>
    /// Returns the occupant of the chamber's existing medical scanner container.
    /// </summary>
    public EntityUid? GetPatient(EntityUid chamber) =>
        TryComp<MedicalScannerComponent>(chamber, out var scanner) ? scanner.BodyContainer.ContainedEntity : null;

    /// <summary>
    /// Resolves the treatment solution in the inserted dispenser-compatible container.
    /// </summary>
    public bool TryGetTreatmentSolution(Entity<GeneticChamberComponent?> chamber, out Entity<SolutionComponent> solution)
    {
        solution = default;
        if (!Resolve(chamber, ref chamber.Comp, logMissing: false))
            return false;
        var beaker = _slots.GetItemOrNull(chamber.Owner, chamber.Comp.BeakerSlot);
        if (beaker is null || !_solutions.TryGetFitsInDispenser(beaker.Value, out var entity, out _))
            return false;
        solution = entity.Value;
        return true;
    }

    /// <summary>
    /// Returns the inserted vial, if any.
    /// </summary>
    public bool TryGetVial(Entity<GeneticChamberComponent?> chamber, out Entity<GeneticVialComponent> vial)
    {
        vial = default;
        if (!Resolve(chamber, ref chamber.Comp, logMissing: false) ||
            _slots.GetItemOrNull(chamber.Owner, chamber.Comp.VialSlot) is not { } uid ||
            !TryComp<GeneticVialComponent>(uid, out var contents))
            return false;

        vial = (uid, contents);
        return true;
    }

    /// <summary>
    /// Returns whether the vial holds genes that the patient can take and does not yet have.
    /// </summary>
    public bool CanInject(EntityUid patient, Entity<GeneticVialComponent> vial) =>
        vial.Comp.Genes.Count > 0 && _genomes.CanApplyGenes(patient, vial.Comp.Genes) &&
        vial.Comp.Genes.Any(id => !_genomes.TryGetGeneStatus(patient, id, out _));

    private bool TryBegin(Entity<GeneticChamberComponent?> chamber, EntityUid console, EntityUid patient,
        GeneticProcedure procedure, [NotNullWhen(true)] out GeneticProcedureSettings? settings)
    {
        settings = null;
        if (!Resolve(chamber, ref chamber.Comp, logMissing: false) || TerminatingOrDeleted(chamber) ||
            chamber.Comp.Patient is not null || TerminatingOrDeleted(console) || TerminatingOrDeleted(patient) ||
            !_power.IsPowered(chamber.Owner) || !Transform(chamber).Anchored || GetPatient(chamber) != patient ||
            !chamber.Comp.Procedures.TryGetValue(procedure, out var found) ||
            found.Duration <= TimeSpan.Zero || found.Dose <= FixedPoint2.Zero ||
            !TryGetTreatmentSolution(chamber, out var solution) ||
            solution.Comp.Solution.GetReagentQuantity(new ReagentId(found.Reagent.Id, null)) < found.Dose)
            return false;

        settings = found;
        return true;
    }

    private void Begin(Entity<GeneticChamberComponent> chamber, EntityUid console, EntityUid patient,
        GeneticProcedure procedure, GeneticProcedureSettings settings)
    {
        chamber.Comp.Console = console;
        chamber.Comp.Patient = patient;
        chamber.Comp.Beaker = _slots.GetItemOrNull(chamber.Owner, chamber.Comp.BeakerSlot);
        chamber.Comp.Operation = procedure;
        chamber.Comp.Consumed = FixedPoint2.Zero;
        chamber.Comp.Started = _timing.CurTime;
        chamber.Comp.Ends = _timing.CurTime + settings.Duration;
        EnsureComp<ActiveGeneticChamberComponent>(chamber);
    }

    /// <summary>
    /// Starts treatment of the expected occupant when the gene state and reagent dose permit it.
    /// </summary>
    public bool TryStartProcedure(
        Entity<GeneticChamberComponent?> chamber,
        EntityUid console,
        EntityUid patient,
        EntityUid user,
        ProtoId<GenePrototype> gene,
        GeneticProcedure procedure)
    {
        if (procedure == GeneticProcedure.Inject ||
            !_genomes.TryGetGeneStatus(patient, gene, out var status) || !procedure.CanProcess(status) ||
            procedure == GeneticProcedure.Activate && !_genomes.IsGeneCompatible(patient, gene) ||
            !TryBegin(chamber, console, patient, procedure, out var settings))
            return false;

        chamber.Comp!.Gene = gene;
        chamber.Comp.InitialStatus = status;
        Begin((chamber.Owner, chamber.Comp), console, patient, procedure, settings);
        _logs.Add(LogType.Action, LogImpact.Medium,
            $"{ToPrettyString(user)} started genetic {procedure} of {gene} on {ToPrettyString(patient)} using {ToPrettyString(chamber)}.");
        return true;
    }

    /// <summary>
    /// Starts injecting the inserted vial's genes into the expected occupant.
    /// </summary>
    public bool TryStartInjection(Entity<GeneticChamberComponent?> chamber, EntityUid console, EntityUid patient,
        EntityUid user)
    {
        if (!TryGetVial(chamber, out var vial) || !CanInject(patient, vial) ||
            !TryBegin(chamber, console, patient, GeneticProcedure.Inject, out var settings))
            return false;

        chamber.Comp!.Vial = vial.Owner;
        chamber.Comp.VialRevision = vial.Comp.Revision;
        Begin((chamber.Owner, chamber.Comp), console, patient, GeneticProcedure.Inject, settings);
        _logs.Add(LogType.Action, LogImpact.Medium,
            $"{ToPrettyString(user)} started injecting {string.Join(", ", vial.Comp.Genes)} into {ToPrettyString(patient)} using {ToPrettyString(chamber)}.");
        return true;
    }

    [SubscribeLocalEvent]
    private void OnRemoved(Entity<GeneticChamberComponent> chamber, ref EntRemovedFromContainerMessage args)
    {
        if (args.Entity == chamber.Comp.Patient || args.Entity == chamber.Comp.Beaker)
            CancelProcedure(chamber.AsNullable());
    }

    [SubscribeLocalEvent]
    private void OnShutdown(Entity<GeneticChamberComponent> chamber, ref ComponentShutdown args) => CancelProcedure(chamber.AsNullable());

    [SubscribeLocalEvent]
    private void OnChamberPower(Entity<GeneticChamberComponent> chamber, ref PowerChangedEvent args)
    {
        if (!args.Powered)
            CancelProcedure(chamber.AsNullable());
    }

    [SubscribeLocalEvent]
    private void OnChamberAnchor(Entity<GeneticChamberComponent> chamber, ref AnchorStateChangedEvent args)
    {
        if (!args.Anchored)
            CancelProcedure(chamber.AsNullable());
    }

    /// <summary>
    /// Ends processing without refunding consumed reagent and releases the controller's active reference.
    /// </summary>
    public void CancelProcedure(Entity<GeneticChamberComponent?> chamber)
    {
        if (!Resolve(chamber, ref chamber.Comp, logMissing: false))
            return;
        var console = chamber.Comp.Console;
        chamber.Comp.Patient = null;
        chamber.Comp.Console = null;
        chamber.Comp.Beaker = null;
        chamber.Comp.Vial = null;
        chamber.Comp.VialRevision = 0;
        chamber.Comp.Consumed = FixedPoint2.Zero;
        RemComp<ActiveGeneticChamberComponent>(chamber);
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

        var chambers = EntityQueryEnumerator<ActiveGeneticChamberComponent, GeneticChamberComponent, MetaDataComponent>();
        while (chambers.MoveNext(out var uid, out _, out var chamber, out var metadata))
        {
            if (!metadata.EntityPaused)
                Process((uid, chamber));
        }
    }

    private void Process(Entity<GeneticChamberComponent> chamber)
    {
        var component = chamber.Comp;
        if (component.Console is not { } console || TerminatingOrDeleted(console) ||
            GetPatient(chamber) != component.Patient || component.Patient is not { } patient || TerminatingOrDeleted(patient) ||
            _slots.GetItemOrNull(chamber.Owner, component.BeakerSlot) != component.Beaker ||
            !IsOperationValid(chamber, patient) ||
            !component.Procedures.TryGetValue(component.Operation, out var settings) || !TryGetTreatmentSolution(chamber.AsNullable(), out var solution))
        {
            CancelProcedure(chamber.AsNullable());
            return;
        }

        var progress = Math.Clamp((_timing.CurTime - component.Started).TotalSeconds / settings.Duration.TotalSeconds, 0, 1);
        var required = FixedPoint2.New(settings.Dose.Double() * progress) - component.Consumed;
        if (solution.Comp.Solution.GetReagentQuantity(new ReagentId(settings.Reagent.Id, null)) < required)
        {
            CancelProcedure(chamber.AsNullable());
            return;
        }
        component.Consumed += _solutions.RemoveReagent(solution, settings.Reagent.Id, required);
        if (_timing.CurTime < component.Ends)
            return;

        var changed = component.Operation switch
        {
            GeneticProcedure.Activate => _genomes.TryActivateGene(patient, component.Gene),
            GeneticProcedure.Deactivate => _genomes.TryDeactivateGene(patient, component.Gene),
            GeneticProcedure.Remove => _genomes.TryRemoveGene(patient, component.Gene),
            GeneticProcedure.Inject => TryFinishInjection(chamber, patient),
            _ => false
        };
        if (changed && component.Operation != GeneticProcedure.Inject)
        {
            _logs.Add(LogType.Action, LogImpact.Medium,
                $"{ToPrettyString(chamber)} completed genetic {component.Operation} of {component.Gene} on {ToPrettyString(patient)}.");
        }

        CancelProcedure(chamber.AsNullable());
    }

    private bool IsOperationValid(Entity<GeneticChamberComponent> chamber, EntityUid patient)
    {
        var component = chamber.Comp;
        if (component.Operation == GeneticProcedure.Inject)
        {
            return TryGetVial(chamber.AsNullable(), out var vial) && vial.Owner == component.Vial &&
                vial.Comp.Revision == component.VialRevision && CanInject(patient, vial);
        }

        return _genomes.TryGetGeneStatus(patient, component.Gene, out var status) && status == component.InitialStatus &&
            (component.Operation != GeneticProcedure.Activate || _genomes.IsGeneCompatible(patient, component.Gene));
    }

    private bool TryFinishInjection(Entity<GeneticChamberComponent> chamber, EntityUid patient)
    {
        if (!TryGetVial(chamber.AsNullable(), out var vial) ||
            !_genomes.TryApplyGenes(patient, vial.Comp.Genes, out var changed) || changed == 0)
            return false;

        _logs.Add(LogType.Action, LogImpact.Medium,
            $"{ToPrettyString(chamber)} injected {string.Join(", ", vial.Comp.Genes)} into {ToPrettyString(patient)}.");
        _vials.Clear(vial);
        return true;
    }
}
