using Content.Shared.Station.Systems;
using Content.Shared.StationRecords;
using Content.Shared.StationRecords.Components;
using Content.Shared.StationRecords.Systems;
using Robust.Client.UserInterface;
using Content.Shared._RedStar.Records; // RS14

namespace Content.Client.StationRecords;

public sealed partial class GeneralStationRecordConsoleBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private GeneralStationRecordConsoleWindow? _window;

    [Dependency] private StationSystem _stationSys = default!;
    [Dependency] private StationRecordsSystem _recordsSys = default!;

    public GeneralStationRecordConsoleBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<GeneralStationRecordConsoleWindow>();
        _window.OnKeySelected += SelectStationRecord;
        _window.OnFiltersChanged += SetStationRecordFilter;
        _window.OnDeleted += id => SendPredictedMessage(new DeleteStationRecord(id));
        Update();
    }

    protected override void ReceiveMessage(BoundUserInterfaceMessage message)
    {
        base.ReceiveMessage(message);
        if (message is not DossierStateMessage state)
            return;

        _window?.UpdateDossier(state.Dossier, state.Section);
    }

    public override void Update()
    {
        base.Update();

        if (!EntMan.TryGetComponent(Owner, out GeneralStationRecordConsoleComponent? comp))
            return;

        var owningStation = _stationSys.GetOwningStation(Owner);

        if (!EntMan.TryGetComponent(owningStation, out StationRecordsComponent? stationRecords))
            return;

        var listing = _recordsSys.BuildListing((owningStation.Value, stationRecords), comp.Filter);

        GeneralStationRecord? record = null;
        if (comp.ActiveKey != null)
        {
            var key = new StationRecordKey(comp.ActiveKey.Value, owningStation.Value);
            _recordsSys.TryGetRecord(key, out record, stationRecords);
        }

        _window?.UpdateState(comp.ActiveKey, record, listing, comp.Filter, comp.CanDeleteEntries);
        SendMessage(new DossierRequestMessage(comp.ActiveKey)); // RS14
    }

    private void SelectStationRecord(uint? key)
    {
        if (!EntMan.TryGetComponent(Owner, out GeneralStationRecordConsoleComponent? comp))
            return;

        comp.ActiveKey = key;
        Update();
    }

    private void SetStationRecordFilter(StationRecordFilterType type, string value)
    {
        if (!EntMan.TryGetComponent(Owner, out GeneralStationRecordConsoleComponent? comp))
            return;

        comp.Filter = new StationRecordsFilter(type, value);
        Update();
    }
}
