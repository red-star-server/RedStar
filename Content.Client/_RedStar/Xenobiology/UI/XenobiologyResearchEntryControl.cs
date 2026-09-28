using System.Numerics;
using Content.Shared._RedStar.Xenobiology;
using Robust.Client.Graphics;
using Robust.Client.UserInterface.Controls;

namespace Content.Client._RedStar.Xenobiology.UI;

/// <summary>
/// An active research goal, updated without replacing its controls or disturbing scroll position.
/// </summary>
public sealed class XenobiologyResearchEntryControl : PanelContainer
{
    private static readonly Color Accent = Color.FromHex("#9FD6AD");
    private readonly XenobiologyResearchEntry _target;
    private readonly XenobiologyNameLabel _status;
    private readonly StyleBoxFlat _panel;

    public XenobiologyResearchEntryControl(XenobiologyResearchEntry target, string name, Texture icon)
    {
        _target = target;
        SetWidth = 190;
        MinHeight = 200;
        _panel = new StyleBoxFlat { BackgroundColor = Color.FromHex("#20242B") };
        PanelOverride = _panel;
        var content = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            SeparationOverride = 8,
            Margin = new Thickness(12),
            HorizontalExpand = true
        };
        content.AddChild(new TextureRect
        {
            Texture = icon,
            SetSize = new Vector2(64, 64),
            Stretch = TextureRect.StretchMode.KeepAspectCentered,
            HorizontalAlignment = HAlignment.Center
        });
        content.AddChild(new XenobiologyNameLabel
        {
            FullText = name,
            Align = Label.AlignMode.Center,
            MinHeight = 26
        });
        content.AddChild(new Label
        {
            Text = Loc.GetString("xenobiology-analyzer-reward", ("points", target.Reward)),
            StyleClasses = { "LabelHeading" },
            Align = Label.AlignMode.Center,
            FontColorOverride = Accent
        });
        _status = new XenobiologyNameLabel { StyleClasses = { "LabelSubText" }, Align = Label.AlignMode.Center };
        content.AddChild(_status);
        AddChild(content);
    }

    public void UpdateState(XenobiologySampleAnalyzerUiState state)
    {
        var matched = _target.Sample == state.SamplePrototype &&
                      state.Status is XenobiologySampleStatus.Ready or XenobiologySampleStatus.Analyzing;
        _panel.BorderColor = matched ? Accent : Color.FromHex("#414854");
        // Reserve the same border width so highlighting never shifts the contents.
        _panel.BorderThickness = new Thickness(2);
        _status.FullText = Loc.GetString(matched
            ? state.Status == XenobiologySampleStatus.Analyzing
                ? "xenobiology-analyzer-analyzing"
                : "xenobiology-analyzer-card-matched"
            : "xenobiology-analyzer-card-available");
        _status.FontColorOverride = matched ? Accent : Color.FromHex("#AAB2BC");
    }
}
