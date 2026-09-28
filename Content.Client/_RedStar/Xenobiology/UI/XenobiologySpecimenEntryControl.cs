using System.Numerics;
using Content.Shared._RedStar.Xenobiology;
using Robust.Client.Graphics;
using Robust.Client.UserInterface.Controls;

namespace Content.Client._RedStar.Xenobiology.UI;

/// <summary>
/// A compact specimen row with server-authoritative selection.
/// </summary>
public sealed class XenobiologySpecimenEntryControl : ContainerButton
{
    public XenobiologySpecimenEntryControl(XenobiologySlimeEntry entry, Texture? icon, bool selected, Action onSelected)
    {
        AddStyleClass(StyleClassButton);
        HorizontalExpand = true;
        ToggleMode = true;
        Pressed = selected;
        ToolTip = entry.DisplayName;

        var row = new BoxContainer { SeparationOverride = 8, Margin = new Thickness(4), HorizontalExpand = true };
        row.AddChild(new TextureRect
        {
            Texture = icon,
            SetSize = new Vector2(40, 40),
            Stretch = TextureRect.StretchMode.KeepAspectCentered
        });
        var text = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            HorizontalExpand = true,
            VerticalAlignment = VAlignment.Center,
            SeparationOverride = 2
        };
        text.AddChild(new XenobiologyNameLabel { FullText = entry.DisplayName });
        if (entry.Stage is { } stage)
        {
            text.AddChild(new Label
            {
                Text = Loc.GetString(stage == SlimeStage.Baby ? "slime-scan-stage-baby" : "slime-scan-stage-adult"),
                StyleClasses = { "LabelSubText" }
            });
        }

        row.AddChild(text);
        AddChild(row);
        OnPressed += _ =>
        {
            Pressed = selected;
            onSelected();
        };
    }
}
