using Content.Shared._RedStar.Botany;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client._RedStar.Botany;

[UsedImplicitly]
public sealed class RedStarPlantAnalyzerBoundUserInterface : BoundUserInterface
{
    private RedStarPlantAnalyzerWindow? _window;

    public RedStarPlantAnalyzerBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();
        _window ??= this.CreateWindow<RedStarPlantAnalyzerWindow>();
        _window.OnAdvancedModeChanged -= OnAdvancedModeChanged;
        _window.OnAdvancedModeChanged += OnAdvancedModeChanged;
    }

    private void OnAdvancedModeChanged(bool advanced)
    {
        SendMessage(new RedStarPlantAnalyzerSetMode(advanced));
    }

    protected override void ReceiveMessage(BoundUserInterfaceMessage message)
    {
        if (message is RedStarPlantAnalyzerReport report)
            _window?.Populate(report);
    }
}
