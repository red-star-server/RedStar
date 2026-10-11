using Content.Shared.MedicalScanner;

namespace Content.Client.HealthAnalyzer.UI;

public sealed partial class HealthAnalyzerBoundUserInterface
{
    private NetEntity? _triageTarget;

    private void InitializeTriage()
    {
        _window!.OnPriorityChanged += priority =>
            SendMessage(new MedicalTriageSetPriorityMessage(priority, _triageTarget));
    }

    private void UpdateTriageTarget(HealthAnalyzerUiState state) => _triageTarget = state.TargetEntity;
}
