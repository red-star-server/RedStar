using System.Linq;
using Content.Server._RedStar.Genetics.Components;
using Content.Server.Administration.Logs;
using Content.Server.Medical.Components;
using Content.Server.Power.EntitySystems;
using Content.Shared._RedStar.Genetics.Components;
using Content.Shared._RedStar.Genetics.Data;
using Content.Shared._RedStar.Genetics.Events;
using Content.Shared._RedStar.Genetics.Prototypes;
using Content.Shared._RedStar.Genetics.UI;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Database;
using Content.Shared.DeviceLinking.Systems;
using Content.Shared.DeviceLinking.Events;
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
    [Dependency] private DeviceLinkSystem _links = default!;
    [Dependency] private SharedSolutionContainerSystem _solutions = default!;
    [Dependency] private ItemSlotsSystem _slots = default!;
    [Dependency] private SharedUserInterfaceSystem _ui = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IAdminLogManager _logs = default!;
    [Dependency] private SharedAudioSystem _audio = default!;

    private TimeSpan _nextUpdate;

    private bool TryGetChamber(Entity<GeneticsConsoleComponent> console, out Entity<GeneticChamberComponent> chamber)
    {
        chamber = default;
        if (!this.IsPowered(console.Owner, EntityManager) || !Transform(console).Anchored)
            return false;
        var linked = _links.GetLinkedSinks(console.Owner, GeneticsConsoleComponent.ChamberPort);
        if (linked.Count != 1)
            return false;
        var uid = linked.First();
        if (TerminatingOrDeleted(uid) || !TryComp<GeneticChamberComponent>(uid, out var component) ||
            !Transform(uid).Anchored || !this.IsPowered(uid, EntityManager) ||
            !_transform.InRange(Transform(console).Coordinates, Transform(uid).Coordinates, console.Comp.MaxDistance))
            return false;
        chamber = (uid, component);
        return true;
    }

    private EntityUid? GetPatient(EntityUid chamber) =>
        TryComp<MedicalScannerComponent>(chamber, out var scanner) ? scanner.BodyContainer.ContainedEntity : null;

    private bool TryGetSolution(Entity<GeneticChamberComponent> chamber, out Entity<SolutionComponent> solution)
    {
        solution = default;
        var beaker = _slots.GetItemOrNull(chamber.Owner, chamber.Comp.BeakerSlot);
        if (beaker is null || !_solutions.TryGetFitsInDispenser(beaker.Value, out var entity, out _))
            return false;
        solution = entity.Value;
        return true;
    }

    [SubscribeLocalEvent]
    private void OnProcedure(Entity<GeneticsConsoleComponent> console, ref GeneticsProcedureMessage args)
    {
        if (!TryGetChamber(console, out var chamber) || chamber.Comp.Patient is not null ||
            GetPatient(chamber) is not { } patient || GetNetEntity(patient) != args.Patient ||
            !_genomes.TryGetGeneStatus(patient, args.Gene, out var status) || !args.Procedure.CanProcess(status) ||
            !chamber.Comp.Procedures.TryGetValue(args.Procedure, out var settings) ||
            settings.Duration <= TimeSpan.Zero || settings.Dose <= FixedPoint2.Zero ||
            !TryGetSolution(chamber, out var solution) || solution.Comp.Solution.GetReagentQuantity(new ReagentId(settings.Reagent.Id, null)) < settings.Dose)
            return;

        chamber.Comp.Console = console;
        chamber.Comp.Patient = patient;
        chamber.Comp.Beaker = _slots.GetItemOrNull(chamber.Owner, chamber.Comp.BeakerSlot);
        chamber.Comp.Gene = args.Gene;
        chamber.Comp.InitialStatus = status;
        chamber.Comp.Operation = args.Procedure;
        chamber.Comp.Consumed = FixedPoint2.Zero;
        chamber.Comp.NextSound = _timing.CurTime;
        chamber.Comp.Started = _timing.CurTime;
        chamber.Comp.Ends = _timing.CurTime + settings.Duration;
        _logs.Add(LogType.Action, LogImpact.Medium,
            $"{ToPrettyString(args.Actor)} started genetic {args.Procedure} of {args.Gene} on {ToPrettyString(patient)} using {ToPrettyString(chamber)}.");
        UpdateConsole(console);
    }

    [SubscribeLocalEvent]
    private void OnCancel(Entity<GeneticsConsoleComponent> console, ref GeneticsCancelMessage args)
    {
        if (TryGetChamber(console, out var chamber) && chamber.Comp.Console == console.Owner)
            Cancel(chamber);
        UpdateConsole(console);
    }

    [SubscribeLocalEvent]
    private void OnRemoved(Entity<GeneticChamberComponent> chamber, ref EntRemovedFromContainerMessage args)
    {
        if (args.Entity == chamber.Comp.Patient || args.Entity == chamber.Comp.Beaker)
            Cancel(chamber);
    }

    [SubscribeLocalEvent]
    private void OnShutdown(Entity<GeneticChamberComponent> chamber, ref ComponentShutdown args) => Cancel(chamber);

    [SubscribeLocalEvent]
    private void OnChamberPower(Entity<GeneticChamberComponent> chamber, ref PowerChangedEvent args)
    {
        if (!args.Powered)
            Cancel(chamber);
    }

    [SubscribeLocalEvent]
    private void OnChamberAnchor(Entity<GeneticChamberComponent> chamber, ref AnchorStateChangedEvent args)
    {
        if (!args.Anchored)
            Cancel(chamber);
    }

    [SubscribeLocalEvent]
    private void OnDisconnected(Entity<GeneticsConsoleComponent> console, ref SourcePortDisconnectedEvent args)
    {
        if (args.Port == GeneticsConsoleComponent.ChamberPort)
            CancelControlledProcedures(console.Owner);
    }

    [SubscribeLocalEvent]
    private void OnConsolePower(Entity<GeneticsConsoleComponent> console, ref PowerChangedEvent args)
    {
        if (!args.Powered)
            CancelControlledProcedures(console.Owner);
    }

    [SubscribeLocalEvent]
    private void OnConsoleAnchor(Entity<GeneticsConsoleComponent> console, ref AnchorStateChangedEvent args)
    {
        if (!args.Anchored)
            CancelControlledProcedures(console.Owner);
    }

    [SubscribeLocalEvent]
    private void OnConsoleShutdown(Entity<GeneticsConsoleComponent> console, ref ComponentShutdown args) =>
        CancelControlledProcedures(console.Owner);

    private void CancelControlledProcedures(EntityUid console)
    {
        var query = EntityQueryEnumerator<GeneticChamberComponent>();
        while (query.MoveNext(out var uid, out var chamber))
        {
            if (chamber.Console == console)
                Cancel((uid, chamber));
        }
    }

    private static void Cancel(Entity<GeneticChamberComponent> chamber)
    {
        chamber.Comp.Patient = null;
        chamber.Comp.Console = null;
        chamber.Comp.Beaker = null;
        chamber.Comp.Consumed = FixedPoint2.Zero;
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
        var consoles = EntityQueryEnumerator<GeneticsConsoleComponent, MetaDataComponent>();
        while (consoles.MoveNext(out var uid, out var console, out var metadata))
        {
            if (!metadata.EntityPaused)
                UpdateConsole((uid, console));
        }
    }

    private void Process(Entity<GeneticChamberComponent> chamber)
    {
        var component = chamber.Comp;
        if (component.Console is not { } console || !TryComp<GeneticsConsoleComponent>(console, out var controller) ||
            !TryGetChamber((console, controller), out var connected) || connected.Owner != chamber.Owner ||
            GetPatient(chamber) != component.Patient || component.Patient is not { } patient || TerminatingOrDeleted(patient) ||
            _slots.GetItemOrNull(chamber.Owner, component.BeakerSlot) != component.Beaker ||
            !_genomes.TryGetGeneStatus(patient, component.Gene, out var status) || status != component.InitialStatus ||
            !component.Procedures.TryGetValue(component.Operation, out var settings) || !TryGetSolution(chamber, out var solution))
        {
            Cancel(chamber);
            return;
        }

        var progress = Math.Clamp((_timing.CurTime - component.Started).TotalSeconds / settings.Duration.TotalSeconds, 0, 1);
        var required = FixedPoint2.New(settings.Dose.Double() * progress) - component.Consumed;
        if (solution.Comp.Solution.GetReagentQuantity(new ReagentId(settings.Reagent.Id, null)) < required)
        {
            Cancel(chamber);
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
            _logs.Add(LogType.Action, LogImpact.Medium,
                $"{ToPrettyString(chamber)} completed genetic {component.Operation} of {component.Gene} on {ToPrettyString(patient)}.");
        Cancel(chamber);
    }

    private void UpdateConsole(Entity<GeneticsConsoleComponent> console)
    {
        if (!_ui.IsUiOpen(console.Owner, GeneticsConsoleUiKey.Key))
            return;
        GeneticAnalyzerUiState? profile = null;
        var procedures = new List<GeneticProcedureInfo>();
        List<ReagentQuantity>? beaker = null;
        var status = GeneticsConsoleStatus.NoChamber;
        var busy = false;
        var progress = 0f;
        ProtoId<GenePrototype>? gene = null;
        GeneticProcedure? operation = null;
        if (TryGetChamber(console, out var chamber))
        {
            status = GeneticsConsoleStatus.NoPatient;
            if (GetPatient(chamber) is { } patient && _genomes.TryGetSnapshot(patient, out var snapshot) &&
                TryComp<GenomeComponent>(patient, out var genome))
            {
                profile = new GeneticAnalyzerUiState(GetNetEntity(patient), true, _genomes.GetLoad(patient), genome.MaximumLoad,
                    snapshot.Genes.OrderBy(id => id.Id, StringComparer.Ordinal)
                        .Select(id => new GeneticAnalyzerGene(id, _genomes.TryGetGeneStatus(patient, id, out var state) ? state : null)).ToList());
                status = GeneticsConsoleStatus.Ready;
            }
            if (TryGetSolution(chamber, out var solution))
            {
                beaker = solution.Comp.Solution.Contents.ToList();
            }
            foreach (var (kind, settings) in chamber.Comp.Procedures)
                procedures.Add(new GeneticProcedureInfo(kind, settings.Reagent,
                    settings.Dose, solution.Comp is null ? FixedPoint2.Zero : solution.Comp.Solution.GetReagentQuantity(new ReagentId(settings.Reagent.Id, null)),
                    (float) settings.Duration.TotalSeconds));
            busy = chamber.Comp.Patient is not null;
            if (busy)
            {
                status = GeneticsConsoleStatus.Working;
                gene = chamber.Comp.Gene;
                operation = chamber.Comp.Operation;
                progress = (float) Math.Clamp((_timing.CurTime - chamber.Comp.Started).TotalSeconds /
                    (chamber.Comp.Ends - chamber.Comp.Started).TotalSeconds, 0, 1);
            }
        }
        _ui.SetUiState(console.Owner, GeneticsConsoleUiKey.Key,
            new GeneticsConsoleUiState(profile, status, procedures, beaker, busy, progress, gene, operation));
    }
}
