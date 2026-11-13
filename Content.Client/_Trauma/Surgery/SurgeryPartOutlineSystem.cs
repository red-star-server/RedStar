using Content.Client.Gameplay;
using Content.Client.Viewport;
using Content.Shared._Trauma.Surgery;
using Content.Shared._Trauma.Surgery.Graph;
using Content.Shared.Body;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Tag;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.Player;
using Robust.Client.State;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using static Robust.Client.Graphics.RSI;

namespace Content.Client._Trauma.Surgery;

/// <summary>
/// Outlines the body part under the cursor while a surgery tool is held, so the player can see
/// exactly which part a click will operate on. Duplicates the part's visual layer on the body
/// sprite and applies the selection outline shader to the copy.
/// </summary>
public sealed partial class SurgeryPartOutlineSystem : EntitySystem
{
    private static readonly ProtoId<ShaderPrototype> OutlineShader = "SelectionOutlineInrange";
    private static readonly ProtoId<TagPrototype> SurgeryToolTag = "SurgeryTool";
    private const string LayerSuffix = "SurgeryOutline";

    [Dependency] private IInputManager _input = default!;
    [Dependency] private IPlayerManager _player = default!;
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private IStateManager _state = default!;
    [Dependency] private IUserInterfaceManager _ui = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private SpriteSystem _sprite = default!;
    [Dependency] private SurgeryGraphSystem _surgery = default!;
    [Dependency] private TagSystem _tag = default!;
    [Dependency] private EntityQuery<SurgeryTargetComponent> _targetQuery;
    [Dependency] private EntityQuery<SpriteComponent> _spriteQuery;
    [Dependency] private EntityQuery<VisualOrganComponent> _visualQuery;

    private EntityUid? _outlinedBody;
    private Enum? _outlinedLayer;
    private ResPath _lastRsi;
    private StateId _lastState;
    private ShaderInstance? _shader;

    public override void Shutdown()
    {
        ClearOutline();
        _shader?.Dispose();
        _shader = null;
        base.Shutdown();
    }

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        if (GetHoveredPart() is not { } hovered)
        {
            ClearOutline();
            return;
        }

        var (body, part) = hovered;
        if (_visualQuery.CompOrNull(part)?.Layer is not { } layer
            || !_spriteQuery.TryComp(body, out var sprite)
            || !_sprite.LayerMapTryGet(body, layer, out var srcIndex, false))
        {
            ClearOutline();
            return;
        }

        if (_outlinedBody != body || _outlinedLayer != layer)
        {
            ClearOutline();
            _outlinedBody = body;
            _outlinedLayer = layer;
        }

        if (_sprite.LayerGetEffectiveRsi(body, srcIndex) is not { } rsi
            || !_sprite.LayerGetRsiState(body, srcIndex).IsValid)
        {
            ClearOutline();
            return;
        }

        var state = _sprite.LayerGetRsiState(body, srcIndex);
        if (!rsi.TryGetState(state, out _))
        {
            ClearOutline();
            return;
        }

        var mapKey = BuildLayerKey(layer);
        var specifier = new SpriteSpecifier.Rsi(rsi.Path, state.Name!);
        if (_sprite.LayerMapTryGet(body, mapKey, out var index, false))
        {
            if (rsi.Path == _lastRsi && state == _lastState)
                return;

            _sprite.LayerSetSprite(body, index, specifier);
        }
        else
        {
            index = _sprite.AddLayer(body, specifier);
            _sprite.LayerMapSet(body, mapKey, index);
        }

        _lastRsi = rsi.Path;
        _lastState = state;

        _shader ??= CreateShader();
        sprite.LayerSetShader(index, _shader, OutlineShader);
        _sprite.LayerSetVisible(body, index, true);
    }

    private (EntityUid Body, EntityUid Part)? GetHoveredPart()
    {
        if (_player.LocalEntity is not { } player
            || _state.CurrentState is not GameplayStateBase screen
            || _ui.CurrentlyHovered is not IViewportControl vp
            || !_input.MouseScreenPosition.IsValid)
            return null;

        var held = _hands.GetActiveItemOrSelf(player);
        if (!_tag.HasTag(held, SurgeryToolTag))
            return null;

        var map = vp.PixelToMap(_input.MouseScreenPosition.Position);
        var clicked = vp is ScalingViewport svp
            ? screen.GetClickedEntity(map, svp.Eye)
            : screen.GetClickedEntity(map);

        if (clicked is not { } body || !_targetQuery.HasComp(body))
            return null;

        return (body, _surgery.ResolveClickedPart(body, map));
    }

    private void ClearOutline()
    {
        if (_outlinedBody is { } body
            && _outlinedLayer is { } layer
            && _sprite.LayerMapTryGet(body, BuildLayerKey(layer), out var index, false))
        {
            _sprite.LayerSetVisible(body, index, false);
        }

        _outlinedBody = null;
        _outlinedLayer = null;
        _lastRsi = default;
        _lastState = default;
    }

    private ShaderInstance CreateShader()
    {
        var instance = _proto.Index(OutlineShader).InstanceUnique();
        instance.SetParameter("outline_width", 1f);
        return instance;
    }

    private static string BuildLayerKey(Enum layer) => $"{layer}{LayerSuffix}";
}