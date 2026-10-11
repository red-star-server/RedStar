using Content.Shared._RedStar.Records;

namespace Content.Client.StationRecords;

public sealed partial class GeneralStationRecordConsoleBoundUserInterface
{
    protected override void ReceiveMessage(BoundUserInterfaceMessage message)
    {
        base.ReceiveMessage(message);
        if (message is DossierStateMessage state)
            _window?.UpdateDossier(state.Dossier);
    }

    private void RequestDossier(uint? recordId)
    {
        SendMessage(new DossierRequestMessage(recordId));
    }
}
