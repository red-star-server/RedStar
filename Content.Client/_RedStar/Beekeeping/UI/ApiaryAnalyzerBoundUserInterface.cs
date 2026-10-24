using Content.Shared._RedStar.Beekeeping.UI;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client._RedStar.Beekeeping.UI;

[UsedImplicitly]
public sealed class ApiaryAnalyzerBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    private ApiaryAnalyzerWindow? _window;

    protected override void Open()
    {
        base.Open();
        _window = this.CreateWindow<ApiaryAnalyzerWindow>();
        _window.Title = EntMan.GetComponent<MetaDataComponent>(Owner).EntityName;
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);
        if (state is ApiaryAnalyzerState apiary)
            _window?.UpdateState(apiary);
    }
}
