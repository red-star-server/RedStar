using System.Numerics;
using Content.Shared._RedStar.Xenobiology.UI;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Shared.Input;

namespace Content.Client._RedStar.Xenobiology.UI;

/// <summary>
/// A compact, clickable genome track. Segments show observed loci, not invented
/// genetic relationships. The visual code is intentionally independent of UI text.
/// </summary>
public sealed class SequencerGenomeControl : Control
{
    private static readonly Color[] Palette =
    [
        Color.FromHex("#79D3D3"), Color.FromHex("#A8C97B"),
        Color.FromHex("#E9BD7D"), Color.FromHex("#C49BD6"),
        Color.FromHex("#6EA7D1"), Color.FromHex("#D98F9B")
    ];

    private readonly Font _font;
    private SequencerMarkerView[] _markers = [];
    private int _selected = -1;
    public event Action<int>? OnSelect;

    private const float Step = 104;
    private const float OriginX = 24;
    private const float CenterY = 39;

    public SequencerGenomeControl()
    {
        var resource = IoCManager.Resolve<IResourceCache>()
            .GetResource<FontResource>("/EngineFonts/NotoSans/NotoSansMono-Regular.ttf");
        _font = new VectorFont(resource, 13);
        MinSize = new Vector2(340, 98);
        HorizontalExpand = true;
        MouseFilter = MouseFilterMode.Stop;
    }

    public void SetMarkers(SequencerMarkerView[] markers, int selected)
    {
        _markers = markers;
        _selected = selected;
        MinSize = new Vector2(Math.Max(340, OriginX * 2 + markers.Length * Step), 98);
    }

    protected override void KeyBindDown(GUIBoundKeyEventArgs args)
    {
        base.KeyBindDown(args);
        if (args.Handled || args.Function != EngineKeyFunctions.UIClick)
            return;
        var mouse = UserInterfaceManager.MousePositionScaled.Position * UIScale - GlobalPixelPosition;
        var index = (int) MathF.Floor((mouse.X / UIScale - OriginX) / Step);
        if (index < 0 || index >= _markers.Length || mouse.Y / UIScale is < 5 or > 98)
            return;
        OnSelect?.Invoke(index);
        args.Handle();
    }

    protected override void Draw(DrawingHandleScreen handle)
    {
        base.Draw(handle);
        var scale = UIScale;
        var center = CenterY * scale;
        for (var i = 0; i < _markers.Length; i++)
        {
            var marker = _markers[i];
            var x = (OriginX + Step * i + 38) * scale;
            var tint = ColorFor(marker.FamilyCode);
            if (i > 0)
            {
                var prior = (OriginX + Step * (i - 1) + 38) * scale;
                handle.DrawLine(new Vector2(prior + 18 * scale, center),
                    new Vector2(x - 18 * scale, center), Color.FromHex("#4B6573"));
            }
            if (i == _selected)
                handle.DrawCircle(new Vector2(x, center), 29 * scale, Color.FromHex("#344A59"));
            handle.DrawCircle(new Vector2(x, center), 20 * scale, tint);
            handle.DrawCircle(new Vector2(x, center), 11 * scale, Color.FromHex("#152431"));
            handle.DrawLine(new Vector2(x - 8 * scale, center - 8 * scale),
                new Vector2(x + 8 * scale, center + 8 * scale), tint);
            handle.DrawLine(new Vector2(x - 8 * scale, center + 8 * scale),
                new Vector2(x + 8 * scale, center - 8 * scale), tint);
            // A single compact glyph: no floating status ring above the locus.
            var label = marker.FamilyCode.Length >= 4 ? marker.FamilyCode[..4] : marker.FamilyCode;
            handle.DrawString(_font, new Vector2(x - 20 * scale, 75 * scale), label, Color.White);
        }
    }

    private static Color ColorFor(string family)
    {
        var hash = 0;
        foreach (var c in family)
        {
            hash = unchecked(hash * 31 + c);
        }

        return Palette[(int) ((uint) hash % (uint) Palette.Length)];
    }
}
