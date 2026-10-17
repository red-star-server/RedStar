using Content.Shared._RedStar.Records;
using Robust.Client.UserInterface;

namespace Content.Client._RedStar.Records;

public sealed class MedicalRecordsConsoleBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    private MedicalRecordsConsoleWindow? _window;

    protected override void Open()
    {
        base.Open();
        _window = this.CreateWindow<MedicalRecordsConsoleWindow>();
        _window.OnSelected += key => SendMessage(new MedicalRecordSelectMessage(key));
        _window.OnPriorityChanged += priority => SendMessage(new MedicalTriageSetPriorityMessage(priority));
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);
        if (state is MedicalRecordsConsoleState medical)
            _window?.UpdateState(medical);
    }
}
