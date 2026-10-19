using System.Linq;
using Content.Server._RedStar.Genetics.Components;
using Content.Server.Power.EntitySystems;
using Content.Shared._RedStar.Genetics.Data;
using Content.Shared._RedStar.Genetics.Events;
using Content.Shared._RedStar.Genetics.Prototypes;
using Content.Shared._RedStar.Genetics.UI;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.DeviceLinking.Events;
using Content.Shared.DeviceLinking.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.Power;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server._RedStar.Genetics.Systems;

/// <summary>
/// Connects genetic chambers to operator controls and displays their live reports.
/// </summary>
public sealed partial class GeneticsConsoleSystem : EntitySystem
{
    [Dependency] private GeneticChamberSystem _chambers = default!;
    [Dependency] private ServerGenomeSystem _genomes = default!;
    [Dependency] private DeviceLinkSystem _links = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private SharedUserInterfaceSystem _ui = default!;
    [Dependency] private IGameTiming _timing = default!;

    private TimeSpan _nextUpdate;

    public override void Initialize()
    {
        base.Initialize();
        UpdatesBefore.Add(typeof(GeneticChamberSystem));
    }

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

    [SubscribeLocalEvent]
    private void OnProcedure(Entity<GeneticsConsoleComponent> console, ref GeneticsProcedureMessage args)
    {
        if (console.Comp.ActiveChamber is not null || !TryGetChamber(console, out var chamber) ||
            _chambers.GetPatient(chamber) is not { } patient || GetNetEntity(patient) != args.Patient)
            return;
        if (_chambers.TryStartProcedure(chamber, console.Owner, patient, args.Gene, args.Procedure, args.Actor))
            console.Comp.ActiveChamber = chamber.Owner;
        UpdateConsole(console);
    }

    [SubscribeLocalEvent]
    private void OnCancel(Entity<GeneticsConsoleComponent> console, ref GeneticsCancelMessage args)
    {
        CancelProcedure(console);
        UpdateConsole(console);
    }

    [SubscribeLocalEvent]
    private void OnEnded(Entity<GeneticsConsoleComponent> console, ref GeneticProcedureEndedEvent args)
    {
        if (console.Comp.ActiveChamber == args.Chamber)
            console.Comp.ActiveChamber = null;
    }

    [SubscribeLocalEvent]
    private void OnDisconnected(Entity<GeneticsConsoleComponent> console, ref SourcePortDisconnectedEvent args)
    {
        if (args.Port == GeneticsConsoleComponent.ChamberPort)
            CancelProcedure(console);
    }

    [SubscribeLocalEvent]
    private void OnPower(Entity<GeneticsConsoleComponent> console, ref PowerChangedEvent args)
    {
        if (!args.Powered)
            CancelProcedure(console);
    }

    [SubscribeLocalEvent]
    private void OnAnchor(Entity<GeneticsConsoleComponent> console, ref AnchorStateChangedEvent args)
    {
        if (!args.Anchored)
            CancelProcedure(console);
    }

    [SubscribeLocalEvent]
    private void OnShutdown(Entity<GeneticsConsoleComponent> console, ref ComponentShutdown args) => CancelProcedure(console);

    private void CancelProcedure(Entity<GeneticsConsoleComponent> console)
    {
        var active = console.Comp.ActiveChamber;
        console.Comp.ActiveChamber = null;
        if (active is { } uid && TryComp<GeneticChamberComponent>(uid, out var chamber) && chamber.Console == console.Owner)
            _chambers.CancelProcedure((uid, chamber));
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        if (_timing.CurTime < _nextUpdate)
            return;
        _nextUpdate = _timing.CurTime + TimeSpan.FromSeconds(0.5);

        var query = EntityQueryEnumerator<GeneticsConsoleComponent, MetaDataComponent>();
        while (query.MoveNext(out var uid, out var console, out var metadata))
        {
            if (metadata.EntityPaused)
                continue;
            var ent = new Entity<GeneticsConsoleComponent>(uid, console);
            if (console.ActiveChamber is { } active &&
                (!TryGetChamber(ent, out var chamber) || chamber.Owner != active || chamber.Comp.Console != uid))
                CancelProcedure(ent);
            UpdateConsole(ent);
        }
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
            if (_chambers.GetPatient(chamber) is { } patient && _genomes.TryGetReport(patient, out var patientReport))
            {
                profile = patientReport;
                status = GeneticsConsoleStatus.Ready;
            }
            if (_chambers.TryGetTreatmentSolution(chamber, out var solution))
            {
                beaker = solution.Comp.Solution.Contents.ToList();
            }
            foreach (var (kind, settings) in chamber.Comp.Procedures)
            {
                procedures.Add(new GeneticProcedureInfo(kind, settings.Reagent,
                    settings.Dose, solution.Comp is null ? FixedPoint2.Zero : solution.Comp.Solution.GetReagentQuantity(new ReagentId(settings.Reagent.Id, null)),
                    (float) settings.Duration.TotalSeconds));
            }

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
