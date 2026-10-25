using Content.Shared._RedStar.Records;
using Content.Shared.Access;
using Content.Shared.Access.Systems;
using Content.Shared.MedicalScanner;
using Robust.Client.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Client.HealthAnalyzer.UI;

public sealed partial class HealthAnalyzerWindow
{
    public event Action<MedicalTriagePriority>? OnPriorityChanged;
    private IEntityManager _entities = default!;
    private IPlayerManager _players = default!;
    private AccessReaderSystem _access = default!;
    private bool _updating;

    private void InitializeTriage()
    {
        _entities = IoCManager.Resolve<IEntityManager>();
        _players = IoCManager.Resolve<IPlayerManager>();
        _access = _entities.System<AccessReaderSystem>();
        foreach (var priority in Enum.GetValues<MedicalTriagePriority>())
        {
            PriorityOptionButton.AddItem(Loc.GetString($"medical-triage-{priority.ToString().ToLowerInvariant()}"), (int) priority);
        }

        PriorityOptionButton.OnItemSelected += args =>
        {
            if (!_updating)
                OnPriorityChanged?.Invoke((MedicalTriagePriority) args.Id);
        };
    }

    private void UpdateTriage(HealthAnalyzerUiState state)
    {
        var patient = _entities.GetEntity(state.TargetEntity);
        TriageControls.Visible = state.TriageEligible && patient != null;
        if (!TriageControls.Visible)
            return;

        var canEdit = state.ScanMode && _players.LocalEntity is { } actor &&
                      _access.FindAccessTags(actor).Contains(new ProtoId<AccessLevelPrototype>("Medical"));
        _entities.TryGetComponent<MedicalTriageStatusComponent>(patient!.Value, out var triage);
        var priority = triage?.Priority ?? MedicalTriagePriority.None;
        _updating = true;
        PriorityOptionButton.SelectId((int) priority);
        _updating = false;
        PriorityOptionButton.Disabled = !canEdit;
        PriorityIcon.Visible = priority != MedicalTriagePriority.None;
        if (!PriorityIcon.Visible) return;
        {
            PriorityIcon.SetFromSpriteSpecifier(new SpriteSpecifier.Rsi(
                new ResPath("_RedStar/Interface/Misc/triage_icons.rsi"), priority.ToString().ToLowerInvariant()));
        }
    }
}
