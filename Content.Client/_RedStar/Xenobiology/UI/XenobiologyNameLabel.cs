using System.Numerics;
using System.Text;
using Robust.Client.Graphics;
using Robust.Client.UserInterface.Controls;

namespace Content.Client._RedStar.Xenobiology.UI;

/// <summary>
/// A single-line name that fits the available width and retains the full text in its tooltip.
/// </summary>
public sealed class XenobiologyNameLabel : Label
{
    private string _fullText = string.Empty;

    public string FullText
    {
        get => _fullText;
        set
        {
            if (_fullText == value)
                return;

            _fullText = value;
            ToolTip = value;
            Text = value.Replace('\n', ' ').Replace('\r', ' ');
        }
    }

    public XenobiologyNameLabel()
    {
        ClipText = true;
        HorizontalExpand = true;
        MouseFilter = MouseFilterMode.Pass;
    }

    protected override Vector2 ArrangeOverride(Vector2 finalSize)
    {
        var font = FontOverride;
        if (font == null && !TryGetStyleProperty<Font>(StylePropertyFont, out font))
            font = UserInterfaceManager.ThemeDefaults.LabelFont;

        var name = _fullText.Replace('\n', ' ').Replace('\r', ' ');
        var available = finalSize.X * UIScale;
        var width = 0f;
        foreach (var rune in name.EnumerateRunes())
        {
            width += font.GetCharMetrics(rune, UIScale)?.Advance ?? 0;
        }

        if (width > available)
        {
            var ellipsisMetrics = font.GetCharMetrics(new Rune('…'), UIScale);
            var ellipsis = ellipsisMetrics != null ? "…" : "...";
            var ellipsisWidth = ellipsisMetrics?.Advance ??
                (font.GetCharMetrics(new Rune('.'), UIScale)?.Advance ?? 0) * 3;
            var length = 0;
            width = ellipsisWidth;
            foreach (var rune in name.EnumerateRunes())
            {
                width += font.GetCharMetrics(rune, UIScale)?.Advance ?? 0;
                if (width > available)
                    break;
                length += rune.Utf16SequenceLength;
            }

            name = available >= ellipsisWidth ? name[..length].TrimEnd() + ellipsis : string.Empty;
        }

        if (Text != name)
            Text = name;
        return base.ArrangeOverride(finalSize);
    }
}
