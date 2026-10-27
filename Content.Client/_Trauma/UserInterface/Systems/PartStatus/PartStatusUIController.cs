using Content.Client._Trauma.Targeting;
using Content.Client._Trauma.UserInterface.Systems.PartStatus.Widgets;
using Content.Client.Gameplay;
using Content.Client.UserInterface.Systems.Alerts.Widgets;
using Content.Shared._Trauma.Body;
using Content.Shared._Trauma.PartStatus;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Client.UserInterface.Controllers;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Client._Trauma.UserInterface.Systems.PartStatus;

public sealed partial class PartStatusUIController : UIController, IOnStateEntered<GameplayState>, IOnSystemChanged<TargetingSystem>
{
    [Dependency] private IEntityManager _entMan = default!;
    [Dependency] private IEntityNetworkManager _entNet = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IPlayerManager _player = default!;
    private SpriteSystem _sprite = default!;

    private BodyStatusComponent? _comp;
    private PartStatusControl? PartStatusControl;

    public override void Initialize()
    {
        base.Initialize();

        UIManager.OnScreenChanged += screens =>
        {
            try
            {
                if (screens.New?.FindControl<AlertsUI>("Alerts") is {} alerts)
                    AddPartStatusToAlerts(alerts);
            }
            catch (ArgumentException)
            {
                // don't care if it's a non-game screen or whatever
                // This API requires a non-null entity.
            }
        };
    }

    /// <summary>
    /// Connects the controller to the newly loaded targeting system.
    /// </summary>
    public void OnSystemLoaded(TargetingSystem system)
    {
        system.PartStatusStartup += UpdatePartStatusControl;
        system.PartStatusShutdown += RemovePartStatusControl;
        system.PartStatusUpdate += UpdatePartStatusControl;
    }

    /// <summary>
    /// Disconnects the controller from the unloaded targeting system.
    /// </summary>
    public void OnSystemUnloaded(TargetingSystem system)
    {
        system.PartStatusStartup -= UpdatePartStatusControl;
        system.PartStatusShutdown -= RemovePartStatusControl;
        system.PartStatusUpdate -= UpdatePartStatusControl;
    }

    private void AddPartStatusToAlerts(AlertsUI alerts)
    {
        PartStatusControl?.Orphan();
        var control = new PartStatusControl(this);
        control.OnPartStatusClicked += GetPartStatusMessage;
        PartStatusControl = control;
        alerts.PartStatus.AddChild(control);
        UpdateVisibility();
    }

    /// <summary>
    /// Updates medical HUD controls when gameplay begins.
    /// </summary>
    public void OnStateEntered(GameplayState state)
    {
        UpdateVisibility();
    }

    private void UpdateVisibility()
    {
        if (PartStatusControl is not {} control)
            return;

        control.SetVisible(_comp != null);
        if (_comp is {} comp)
            control.SetTextures(comp.BodyStatus);
    }

    /// <summary>
    /// Removes the local body-status control from the HUD.
    /// </summary>
    public void RemovePartStatusControl()
    {
        _comp = null;
        UpdateVisibility();
    }

    /// <summary>
    /// Updates the HUD from the local body-status component.
    /// </summary>
    public void UpdatePartStatusControl(BodyStatusComponent comp)
    {
        _comp = comp;
        UpdateVisibility();
    }

    /// <summary>
    /// Resolves the texture used by a body-status sprite.
    /// </summary>
    public Texture GetTexture(SpriteSpecifier specifier)
    {
        _sprite ??= _entMan.System<SpriteSystem>();

        return _sprite.Frame0(specifier);
    }

    /// <summary>
    /// Requests the local character body-status examination text from the server.
    /// </summary>
    public void GetPartStatusMessage()
    {
        if (_player.LocalEntity is not {} user
            || !_entMan.HasComponent<BodyStatusComponent>(user)
            || !_timing.IsFirstTimePredicted)
            return;

        _entNet.SendSystemNetworkMessage(new GetPartStatusEvent());
    }
}
