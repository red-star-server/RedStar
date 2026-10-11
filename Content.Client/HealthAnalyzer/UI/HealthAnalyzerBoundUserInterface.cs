using Content.Shared.Medical.HealthAnalyzer;
using Content.Shared._RedStar.Records; // RS14
using Content.Shared.MedicalScanner;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client.HealthAnalyzer.UI;

[UsedImplicitly]
public sealed partial class HealthAnalyzerBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    [ViewVariables]
    private HealthAnalyzerWindow? _window;
    private NetEntity? _triageTarget; // RS14

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<HealthAnalyzerWindow>();
        // RS14-start
        _window.OnPriorityChanged += priority => SendMessage(new MedicalTriageSetPriorityMessage(priority, _triageTarget));
        // RS14-end
        _window.Title = EntMan.GetComponent<MetaDataComponent>(Owner).EntityName;
    }

    /// <summary>
    /// This will update the UI to reflect the newest health changes of the scanned entity.
    /// This gets called in the <see cref="HealthAnalyzerSystem"/> by SetUIState().
    /// </summary>
    protected override void UpdateState(BoundUserInterfaceState state)
    {
        if (_window == null || state is not HealthAnalyzerUiState cast)
            return;

        // RS14-start
        _triageTarget = cast.TargetEntity;
        _window.Populate(cast);
        // RS14-end
    }
}
