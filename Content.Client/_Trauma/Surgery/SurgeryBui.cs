using Content.Shared._Trauma.Surgery;
using Robust.Client.UserInterface;

namespace Content.Client._Trauma.Surgery;

public sealed partial class SurgeryBui : BoundUserInterface
{
    [ViewVariables] private SurgeryWindow? _window;

    public SurgeryBui(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        _window = this.CreateWindow<SurgeryWindow>();
        _window.OnPerformStep += (part, surgery, step) => SendPredictedMessage(new SurgeryStepChosenBuiMsg(part, surgery, step));

        _window.SetOwner(Owner);
    }
}
