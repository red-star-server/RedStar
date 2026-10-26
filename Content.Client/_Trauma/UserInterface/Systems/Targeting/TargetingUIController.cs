using Content.Client._Trauma.Targeting;
using Content.Client._Trauma.UserInterface.Systems.Targeting.Widgets;
using Content.Client.Gameplay;
using Content.Shared._Trauma.Common.Targeting;
using Content.Shared._Trauma.Targeting;
using Robust.Client.Player;
using Robust.Client.UserInterface.Controllers;
using Robust.Client.UserInterface.Controls;

namespace Content.Client._Trauma.UserInterface.Systems.Targeting;

public sealed partial class TargetingUIController : UIController, IOnStateEntered<GameplayState>, IOnSystemChanged<TargetingSystem>
{
    [Dependency] private IEntityManager _entMan = default!;
    [Dependency] private IEntityNetworkManager _entNet = default!;
    [Dependency] private IPlayerManager _player = default!;

    private TargetingComponent? _targetingComponent;
    private TargetingControl? TargetingControl;

    public override void Initialize()
    {
        base.Initialize();

        UIManager.OnScreenChanged += screens =>
        {
            if (screens.New is not {} screen)
                return;

            try
            {
                // separated hud has its own container for it
                var container = screen.FindControl<LayoutContainer>("ViewportContainer");
                CreateTargetingControl(container);
            }
            catch (ArgumentException)
            {
                // default hud, just add it directly
                CreateTargetingControl(screen);
            }
        };
    }

    /// <summary>
    /// Connects the controller to the newly loaded targeting system.
    /// </summary>
    public void OnSystemLoaded(TargetingSystem system)
    {
        system.TargetingStartup += AddTargetingControl;
        system.TargetingShutdown += RemoveTargetingControl;
        system.TargetChange += CycleTarget;
    }

    /// <summary>
    /// Disconnects the controller from the unloaded targeting system.
    /// </summary>
    public void OnSystemUnloaded(TargetingSystem system)
    {
        system.TargetingStartup -= AddTargetingControl;
        system.TargetingShutdown -= RemoveTargetingControl;
        system.TargetChange -= CycleTarget;
    }

    private void CreateTargetingControl(LayoutContainer parent)
    {
        TargetingControl?.Orphan();
        var control = new TargetingControl();
        control.OnSetTarget += CycleTarget;
        parent.AddChild(control);
        TargetingControl = control;
        LayoutContainer.SetAnchorAndMarginPreset(control, LayoutContainer.LayoutPreset.BottomRight, margin: 5);
        UpdateVisibility();
    }

    /// <summary>
    /// Updates medical HUD controls when gameplay begins.
    /// </summary>
    public void OnStateEntered(GameplayState state)
    {
        UpdateVisibility();
    }

    /// <summary>
    /// Adds the targeting control for the local character.
    /// </summary>
    public void AddTargetingControl(TargetingComponent component)
    {
        _targetingComponent = component;
        UpdateVisibility();
    }

    private void UpdateVisibility()
    {
        if (TargetingControl is not {} control)
            return;

        control.SetTargetDollVisible(_targetingComponent != null);

        if (_targetingComponent is {} comp)
            control.SetBodyPartsVisible(comp.Target);
    }

    /// <summary>
    /// Removes the local targeting control from the HUD.
    /// </summary>
    public void RemoveTargetingControl()
    {
        _targetingComponent = null;
        UpdateVisibility();
    }

    /// <summary>
    /// Selects the requested target body part through the targeting system.
    /// </summary>
    public void CycleTarget(TargetBodyPart bodyPart)
    {
        if (_player.LocalEntity is not {} user
            || !_entMan.TryGetComponent<TargetingComponent>(user, out var comp)
            || comp.Target == bodyPart
            || TargetingControl is not {} control)
            return;

        _entNet.SendSystemNetworkMessage(new ChangeTargetMessage(bodyPart));
        control.SetBodyPartsVisible(bodyPart);
    }
}
