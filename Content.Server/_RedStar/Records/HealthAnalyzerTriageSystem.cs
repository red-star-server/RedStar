using Content.Shared._RedStar.Records;
using Content.Shared.Access.Systems;
using Content.Shared.Access;
using Content.Shared.Medical.HealthAnalyzer;
using Content.Shared.MedicalScanner;
using Content.Shared.StationRecords;
using Content.Shared.StationRecords.Systems;
using Robust.Shared.Prototypes;

namespace Content.Server._RedStar.Records;

public sealed partial class HealthAnalyzerTriageSystem : EntitySystem
{
    [Dependency] private AccessReaderSystem _access = default!;
    [Dependency] private StationRecordsSystem _records = default!;
    [Dependency] private MedicalRecordsConsoleSystem _triage = default!;

    public override void Initialize()
    {
        base.Initialize();
        Subs.BuiEvents<HealthAnalyzerComponent>(HealthAnalyzerUiKey.Key, events =>
        {
            events.Event<MedicalTriageSetPriorityMessage>(OnSetPriority);
            events.Event<MedicalTriageClaimMessage>(OnClaim);
        });
    }

    private bool TryGetPatientRecord(Entity<HealthAnalyzerComponent> analyzer, EntityUid actor,
        out StationRecordKey key)
    {
        key = default;
        if (!analyzer.Comp.IsAnalyzerActive || analyzer.Comp.ScannedEntity is not { } patient ||
            !_access.FindAccessTags(actor).Contains(new ProtoId<AccessLevelPrototype>("Medical")) ||
            !_access.FindStationRecordKeys(patient, out var keys))
            return false;

        foreach (var candidate in keys)
        {
            if (!_records.TryGetRecord<GeneralStationRecord>(candidate, out _))
                continue;

            key = candidate;
            return true;
        }

        return false;
    }

    private void OnSetPriority(Entity<HealthAnalyzerComponent> analyzer, ref MedicalTriageSetPriorityMessage args)
    {
        if (TryGetPatientRecord(analyzer, args.Actor, out var key))
            _triage.SetPriority(key, args.Priority);
    }

    private void OnClaim(Entity<HealthAnalyzerComponent> analyzer, ref MedicalTriageClaimMessage args)
    {
        if (TryGetPatientRecord(analyzer, args.Actor, out var key))
            _triage.Claim(key, args.Actor);
    }
}
