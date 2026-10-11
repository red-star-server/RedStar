using Content.Shared.Medical.HealthAnalyzer;
using Content.Shared.MedicalScanner;
using Content.Server._RedStar.Records; // RS14
using Content.Shared.Temperature.Components;

namespace Content.Server.Medical.HealthAnalyzer;

/// <inheritdoc/>
public sealed partial class ServerHealthAnalyzerSystem : HealthAnalyzerSystem
{
    [Dependency] private EntityQuery<TemperatureComponent> _temperatureQuery;
    [Dependency] private MedicalTriageSystem _triage = default!; // RS14

    public override HealthAnalyzerUiState GetHealthAnalyzerUiState(EntityUid? target, bool scanMode)
    {
        var state = base.GetHealthAnalyzerUiState(target, scanMode);
        state.TriageEligible = target is { } patient && _triage.TryGetPatientKey(patient, out _); // RS14

        if (_temperatureQuery.TryComp(target, out var temp))
            state.Temperature = temp.Temperature;

        return state;
    }
}
