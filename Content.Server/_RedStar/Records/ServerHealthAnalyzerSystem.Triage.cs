using Content.Server._RedStar.Records;
using Content.Shared.MedicalScanner;

namespace Content.Server.Medical.HealthAnalyzer;

public sealed partial class ServerHealthAnalyzerSystem
{
    [Dependency] private MedicalTriageSystem _triage = default!;

    private void UpdateTriageEligibility(HealthAnalyzerUiState state, EntityUid? target)
    {
        state.TriageEligible = target is { } patient && _triage.TryGetPatientKey(patient, out _);
    }
}
