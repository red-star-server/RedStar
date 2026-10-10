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
    private CharacterRecordConsoleUpdateMsg? _lastMessage;

    protected override void ReceiveMessage(BoundUserInterfaceMessage message)
    {
        base.ReceiveMessage(message);
        if (message is not CharacterRecordConsoleUpdateMsg state)
            return;

        _lastMessage = state;
        ApplyUpdate(state);
    }

    protected override void Open()
    {
        base.Open();

        _window = new();
        if (_lastMessage is { } lastMessage)
            ApplyUpdate(lastMessage);
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

    private void ApplyUpdate(CharacterRecordConsoleUpdateMsg state)
    {
        if (_window is not { } window)
            return;

        if (state.ConsoleType is RecordConsoleType.Security or RecordConsoleType.Admin &&
            EntMan.TryGetComponent<CriminalRecordsConsoleComponent>(Owner, out var comp))
            window.SecurityWantedStatusMaxLength = comp.MaxStringLength;

        window.UpdateState(state);
        window.SetSecurityStatusEnabled(
            state.ConsoleType is RecordConsoleType.Security or RecordConsoleType.Admin &&
            state.SelectedSecurityStatus != null &&
            EntMan.HasComponent<CriminalRecordsConsoleComponent>(Owner));
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        _window?.Close();
    }
}
