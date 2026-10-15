using System.Linq;
using System.Numerics;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Shared.Input;

namespace Content.Client._RedStar.Xenobiology.UI;

public sealed class SequencerAssemblyControl : Control
{
    private static readonly Color Back = Color.FromHex("#101923");
    private static readonly Color Card = Color.FromHex("#253646");
    private static readonly Color Outline = Color.FromHex("#415568");
    private static readonly Color Accent = Color.FromHex("#73C6CD");
    private static readonly Color Good = Color.FromHex("#78CC9D");
    private static readonly Color Bad = Color.FromHex("#DC826F");
    private static readonly Color Muted = Color.FromHex("#96A9B8");
    private readonly Font _font;
    private readonly int?[] _slots = new int?[4];
    private string[] _fragments = [];
    private string _prefix = string.Empty;
    private int? _drag;
    private int? _origin;
    private Vector2 _cursor;
    private bool _held;
    private bool _feedback;
    private bool _decoded;
    public bool IsDecoded => _decoded;

    private const float CardW = 156;
    private const float CardH = 52;
    private const float Gap = 10;
    private const float Left = 14;
    private const float TrayY = 45;
    private const float BoardY = 142;

    public event Action? AssemblyChanged;

    public SequencerAssemblyControl()
    {
        var resource = IoCManager.Resolve<IResourceCache>()
            .GetResource<FontResource>("/EngineFonts/NotoSans/NotoSansMono-Regular.ttf");
        _font = new VectorFont(resource, 14);
        MinSize = new Vector2(700, 215);
        HorizontalExpand = true;
        MouseFilter = MouseFilterMode.Stop;
    }

    public void SetFragments(string[] fragments, string prefix)
    {
        _fragments = fragments;
        _prefix = prefix;
        Clear(false);
    }

    public void Clear(bool notify = true)
    {
        Array.Fill(_slots, null);
        _drag = null;
        _origin = null;
        _held = false;
        _feedback = false;
        _decoded = false;
        if (notify)
            AssemblyChanged?.Invoke();
    }

    public int[] OrderedFragments => _slots.All(x => x.HasValue)
        ? _slots.Select(x => x!.Value).ToArray()
        : [];

    public bool HasPlaced => _slots.Any(x => x.HasValue);

    public bool Complete => _slots.All(slot => slot.HasValue);

    public void SetFeedback(int[] order)
    {
        _feedback = !_decoded && order.Length == 4 && OrderedFragments.SequenceEqual(order);
    }

    /// <summary>
    /// An already decoded locus stays visible as a read-only, correctly assembled sequence.
    /// </summary>
    public void SetDecoded(bool decoded)
    {
        if (_decoded == decoded)
            return;
        _decoded = decoded;
        _feedback = decoded;
        if (!decoded || _fragments.Length != 4 || _prefix.Length < 4)
            return;

        Array.Fill(_slots, null);
        var used = new bool[4];
        var expected = _prefix[..4];
        for (var position = 0; position < 4; position++)
        {
            var match = -1;
            for (var i = 0; i < _fragments.Length; i++)
            {
                if (!used[i] && _fragments[i].Length >= 7 && _fragments[i][..4] == expected)
                {
                    match = i;
                    break;
                }
            }
            if (match == -1)
                break;
            _slots[position] = match;
            used[match] = true;
            expected = _fragments[match][3..];
        }
    }

    private Vector2 Pointer() =>
        UserInterfaceManager.MousePositionScaled.Position * UIScale - GlobalPixelPosition;

    private static bool Inside(Vector2 point, Vector2 pos, float scale) =>
        point.X >= pos.X && point.X < pos.X + CardW * scale &&
        point.Y >= pos.Y && point.Y < pos.Y + CardH * scale;

    private Vector2 TilePos(int index, float y) =>
        new((Left + index * (CardW + Gap)) * UIScale, y * UIScale);

    private int? HitSlot(Vector2 point, float y)
    {
        for (var i = 0; i < 4; i++)
        {
            if (Inside(point, TilePos(i, y), UIScale))
                return i;
        }

        return null;
    }

    protected override void KeyBindDown(GUIBoundKeyEventArgs args)
    {
        base.KeyBindDown(args);
        if (_decoded || args.Handled || args.Function != EngineKeyFunctions.UIClick || _fragments.Length != 4)
            return;
        var point = Pointer();
        var slot = HitSlot(point, BoardY);
        if (slot is { } target && _slots[target] is { } placed)
        {
            _drag = placed;
            _origin = target;
        }
        else if (HitSlot(point, TrayY) is { } tray && !_slots.Contains(tray))
        {
            _drag = tray;
            _origin = null;
        }
        else
        {
            return;
        }
        _cursor = point;
        _feedback = false;
        _held = true;
        args.Handle();
    }

    protected override void MouseMove(GUIMouseMoveEventArgs args)
    {
        base.MouseMove(args);
        if (_held)
            _cursor = Pointer();
    }

    protected override void KeyBindUp(GUIBoundKeyEventArgs args)
    {
        base.KeyBindUp(args);
        if (_decoded || args.Function != EngineKeyFunctions.UIClick || !_held || _drag is not { } dragged)
            return;
        _held = false;
        var target = HitSlot(Pointer(), BoardY);
        if (target is { } destination)
        {
            var replaced = _slots[destination];
            _slots[destination] = dragged;
            if (_origin is { } origin && origin != destination)
                _slots[origin] = replaced;
        }
        else if (_origin is { } source)
        {
            _slots[source] = null;
        }
        _drag = null;
        _origin = null;
        AssemblyChanged?.Invoke();
        args.Handle();
    }

    protected override void Draw(DrawingHandleScreen handle)
    {
        base.Draw(handle);
        handle.DrawRect(PixelSizeBox, Back);
        if (_fragments.Length != 4)
            return;
        Text(handle, Loc.GetString("cell-sequencer-reads"), new Vector2(Left, 16), Muted);
        Text(handle, Loc.GetString("cell-sequencer-board"), new Vector2(Left, 115), Muted);
        for (var i = 0; i < 4; i++)
        {
            var trayPos = TilePos(i, TrayY);
            var trayBusy = _slots.Contains(i);
            DrawTile(handle, trayPos, trayBusy ? Card : Outline, trayBusy ? "" : _fragments[i], trayBusy);
            var pos = TilePos(i, BoardY);
            var cell = _slots[i];
            var fill = cell.HasValue ? Card : Back;
            DrawTile(handle, pos, fill, cell is { } index ? _fragments[index] : $"{i + 1:00}", !cell.HasValue);
            if (!_feedback || i <= 0 || cell is not { } current || _slots[i - 1] is not { } previous)
                continue;

            var matches = _fragments[previous][3..] == _fragments[current][..4];
            handle.DrawRect(new UIBox2(pos.X - 8 * UIScale, pos.Y + 13 * UIScale,
                pos.X - 4 * UIScale, pos.Y + 39 * UIScale), matches ? Good : Bad);
        }
        if (_held && _drag is { } dragged)
            DrawTile(handle, _cursor - new Vector2(40, 18) * UIScale, Accent, _fragments[dragged], false);
    }

    private void DrawTile(DrawingHandleScreen handle, Vector2 pos, Color fill, string value, bool subdued)
    {
        var box = UIBox2.FromDimensions(pos, new Vector2(CardW, CardH) * UIScale);
        handle.DrawRect(box, fill);
        handle.DrawRect(UIBox2.FromDimensions(pos, new Vector2(CardW, 2) * UIScale), subdued ? Muted : Accent);
        if (value.Length == 0)
            return;

        var first = value[..Math.Min(4, value.Length)];
        var second = value.Length > 4 ? value[4..] : string.Empty;
        Text(handle, first, pos / UIScale + new Vector2(15, 20), subdued ? Muted : Color.White);
        if (second.Length > 0)
            Text(handle, second, pos / UIScale + new Vector2(85, 20), subdued ? Muted : Accent);
    }

    private void Text(DrawingHandleScreen handle, string value, Vector2 pos, Color color) =>
        handle.DrawString(_font, pos * UIScale, value, color);
}
