using Content.Shared._RedStar.Records;
using Content.Shared.Medical.HealthAnalyzer;
using Content.Shared.MedicalScanner;
using Content.Shared.StationRecords;
using Robust.Shared.GameObjects;

namespace Content.Server._RedStar.Records;

public sealed partial class HealthAnalyzerTriageSystem : EntitySystem
{
    [Dependency] private MedicalTriageSystem _triage = default!;
    [Dependency] private SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();
        Subs.BuiEvents<HealthAnalyzerComponent>(HealthAnalyzerUiKey.Key, events =>
        {
            events.Event<MedicalTriageSetPriorityMessage>(OnSetPriority);
        });
    }

    private bool TryGetPatientRecord(Entity<HealthAnalyzerComponent> analyzer, NetEntity? target,
        out StationRecordKey key)
    {
        key = default;
        if (!analyzer.Comp.IsAnalyzerActive || analyzer.Comp.ScannedEntity is not { } patient ||
            Deleted(patient) || target == null || GetNetEntity(patient) != target ||
            (analyzer.Comp.MaxScanRange is { } range &&
             !_transform.InRange(Transform(patient).Coordinates, Transform(analyzer.Owner).Coordinates, range)))
            return false;

        return _triage.TryGetPatientKey(patient, out key);
    }

    private void OnSetPriority(Entity<HealthAnalyzerComponent> analyzer, ref MedicalTriageSetPriorityMessage args)
    {
        if (TryGetPatientRecord(analyzer, args.Target, out var key))
            _triage.SetPriority(key, args.Priority, args.Actor);
    }
}
