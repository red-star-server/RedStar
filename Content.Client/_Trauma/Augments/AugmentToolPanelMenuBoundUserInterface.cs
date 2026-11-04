using Content.Client.UserInterface.Controls;
using Content.Shared._Trauma.Augments;
using Content.Shared.Storage;
using Robust.Client.UserInterface;
using Robust.Shared.Utility;

namespace Content.Client._Trauma.Augments;

public sealed partial class AugmentToolPanelMenuBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    private SimpleRadialMenu? _menu;

    protected override void Open()
    {
        base.Open();

        if (!EntMan.TryGetComponent<StorageComponent>(Owner, out var storage))
            return;

        var options = new List<RadialMenuOptionBase>();
        foreach (var (tool, _) in storage.StoredItems)
        {
            options.Add(new RadialMenuActionOption<EntityUid?>(SendSwitchMessage, tool)
            {
                IconSpecifier = new RadialMenuEntityIconSpecifier(tool),
                ToolTip = EntMan.GetComponent<MetaDataComponent>(tool).EntityName,
                Order = options.Count,
            });
        }

        options.Add(new RadialMenuActionOption<EntityUid?>(SendSwitchMessage, null)
        {
            IconSpecifier = RadialMenuIconSpecifier.With(new SpriteSpecifier.Texture(new ResPath("/Textures/Interface/VerbIcons/close.svg.192dpi.png"))),
            ToolTip = Loc.GetString("augment-tool-panel-retract"),
            Order = options.Count,
        });

        _menu = this.CreateWindow<SimpleRadialMenu>();
        _menu.SetButtons(options);
        _menu.OpenOverMouseScreenPosition();
    }

    /// <summary>
    /// Requests that the server select a stored tool, or retract the current tool when null.
    /// </summary>
    public void SendSwitchMessage(EntityUid? desiredTool)
    {
        SendPredictedMessage(new AugmentToolPanelSwitchMessage(EntMan.GetNetEntity(desiredTool)));
    }
}
