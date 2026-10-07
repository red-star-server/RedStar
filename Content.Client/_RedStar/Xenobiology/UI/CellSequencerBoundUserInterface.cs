using Content.Shared.Research.Components;
using Content.Shared._RedStar.Xenobiology.UI;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client._RedStar.Xenobiology.UI;

[UsedImplicitly]
public sealed class CellSequencerBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    [ViewVariables]
    private CellSequencerWindow? _window;

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<CellSequencerWindow>();
        _window.SetEntity(Owner);
        _window.OnServerSelection += () => SendMessage(new ConsoleServerSelectionMessage());
        _window.OnAdd += (index, revision, dish) => SendMessage(new CellSequencerUiAddMessage(index, revision, dish));
        _window.OnRemove += (id, remote, revision, dish) => SendMessage(new CellSequencerUiRemoveMessage(id, remote, revision, dish));
        _window.OnPrint += cell => SendMessage(new CellSequencerUiReplaceMessage(cell));
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is not CellSequencerUiState sequencerUiState)
            return;

        _window?.UpdateState(sequencerUiState);
    }
}
