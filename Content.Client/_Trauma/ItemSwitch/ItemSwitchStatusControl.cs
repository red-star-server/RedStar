using Content.Client.Items.UI;
using Content.Client.Message;
using Content.Shared._Trauma.ItemSwitch;
using Robust.Client.UserInterface.Controls;

namespace Content.Client._Trauma.ItemSwitch;

public sealed partial class ItemSwitchStatusControl : PollingItemStatusControl<ItemSwitchStatusControl.Data>
{
    [Dependency] private ILocalizationManager _loc = default!;

    private readonly Entity<ItemSwitchComponent> _parent;
    private readonly RichTextLabel _label;

    public ItemSwitchStatusControl(Entity<ItemSwitchComponent> parent)
    {
        IoCManager.InjectDependencies(this);
        _parent = parent;
        _label = new RichTextLabel { StyleClasses = { "ItemStatus" } };
        if (parent.Comp.ShowLabel)
            AddChild(_label);

        UpdateDraw();
    }

    protected override Data PollData()
    {
        return new Data(_parent.Comp.State);
    }

    protected override void Update(in Data data)
    {
        _label.SetMarkup(Loc.GetString("itemswitch-component-on-examine-detailed-message",
            ("state", _loc.TryGetString("trauma-surgery-mode-" + data.State, out var state) ? state : data.State)));
    }

    public record struct Data(string State);
}
