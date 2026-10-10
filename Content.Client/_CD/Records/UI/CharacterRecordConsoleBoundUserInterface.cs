using Content.Shared.CriminalRecords.Components;
using Content.Shared.CriminalRecords;
using Content.Shared.StationRecords;
using Content.Shared._CD.Records;
using JetBrains.Annotations;

namespace Content.Client._CD.Records.UI;

[UsedImplicitly]
public sealed class CharacterRecordConsoleBoundUserInterface(EntityUid owner, Enum key) : BoundUserInterface(owner, key)
{
    [ViewVariables] private CharacterRecordViewer? _window;

    protected override void UpdateState(BoundUserInterfaceState baseState)
    {
        base.UpdateState(baseState);
        if (baseState is not CharacterRecordConsoleState state)
            return;

        if (_window is { } window &&
            (state.ConsoleType is RecordConsoleType.Security or RecordConsoleType.Admin) &&
            EntMan.TryGetComponent<CriminalRecordsConsoleComponent>(Owner, out var comp))
        {
            window.SecurityWantedStatusMaxLength = comp.MaxStringLength;
        }

        _window?.UpdateState(state);
        _window?.SetSecurityStatusEnabled(
            (state.ConsoleType is RecordConsoleType.Security or RecordConsoleType.Admin) &&
            state.SelectedSecurityStatus != null &&
            EntMan.HasComponent<CriminalRecordsConsoleComponent>(Owner));
    }

    protected override void Open()
    {
        base.Open();

        _window = new();
        _window.OnClose += Close;
        _window.OnListingItemSelected += key =>
        {
            SendMessage(new CharacterRecordConsoleSelectMsg(key));
            _window.SetSecurityStatusEnabled(false);
        };

        _window.OnFiltersChanged += (ty, txt) =>
        {
            SendMessage(txt == null
                ? new CharacterRecordsConsoleFilterMsg(null)
                : new CharacterRecordsConsoleFilterMsg(new StationRecordsFilter(ty, txt)));
        };

        _window.OnSetSecurityStatus += (status, reason) =>
        {
            SendMessage(new CriminalRecordChangeStatus(status, reason));
        };

        _window.OpenCentered();
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        _window?.Close();
    }
}
