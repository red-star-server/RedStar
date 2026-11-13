using Content.Shared._Trauma.Surgery.Graph;
using Content.Shared.Body;
using Robust.Client.GameObjects;
using Robust.Client.ResourceManagement;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations;
using Robust.Shared.Utility;

namespace Content.Client._Trauma.Surgery;

public sealed partial class SurgeryVisualsSystem : EntitySystem
{
    [Dependency] private BodySystem _body = default!;
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private IResourceCache _cache = default!;
    [Dependency] private SpriteSystem _sprite = default!;
    [Dependency] private EntityQuery<SpriteComponent> _spriteQuery;
    [Dependency] private EntityQuery<VisualOrganComponent> _visualQuery;

    private const string LayerSuffix = "Surgery";

    [SubscribeLocalEvent]
    private void OnOperatedState(Entity<OperatedComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        UpdateVisuals(ent);
    }

    [SubscribeLocalEvent]
    private void OnOperatedShutdown(Entity<OperatedComponent> ent, ref ComponentShutdown args)
    {
        RemoveOverlays(ent);
    }

    [SubscribeLocalEvent]
    private void OnPartInserted(Entity<OperatedComponent> ent, ref OrganGotInsertedEvent args)
    {
        UpdateVisuals(ent);
    }

    [SubscribeLocalEvent]
    private void OnPartRemoved(Entity<OperatedComponent> ent, ref OrganGotRemovedEvent args)
    {
        if (!TerminatingOrDeleted(args.Target))
            RemoveOverlays(ent);
    }

    private void UpdateVisuals(Entity<OperatedComponent> ent)
    {
        if (GetBodySprite(ent) is not { } body || !_spriteQuery.Resolve(ref body))
            return;

        if (GetLayer(ent) is not { } layer)
            return;

        var overlays = GetNodeOverlays(ent.Comp);
        for (var i = 0; i < overlays.Count; i++)
        {
            var overlay = overlays[i];
            var state = $"{layer}_{overlay.StateSuffix}";
            var rsiPath = SpriteSpecifierSerializer.TextureRoot / new ResPath(overlay.Rsi);

            if (!_cache.TryGetResource<RSIResource>(rsiPath, out var rsi) || !rsi.RSI.TryGetState(state, out _))
                continue;

            var mapKey = BuildLayerKey(layer, i);
            var specifier = new SpriteSpecifier.Rsi(new ResPath(overlay.Rsi), state);
            if (!_sprite.LayerMapTryGet(body, mapKey, out var index, false))
            {
                index = _sprite.AddLayer(body, specifier);
                _sprite.LayerMapSet(body, mapKey, index);
            }
            else
            {
                _sprite.LayerSetSprite(body, index, specifier);
            }

            _sprite.LayerSetVisible(body, index, true);
        }

        for (var i = overlays.Count; _sprite.LayerMapTryGet(body, BuildLayerKey(layer, i), out var index, false); i++)
        {
            _sprite.LayerSetVisible(body, index, false);
        }
    }

    private void RemoveOverlays(Entity<OperatedComponent> ent)
    {
        if (GetBodySprite(ent) is not { } body || !_spriteQuery.Resolve(ref body))
            return;

        if (GetLayer(ent) is not { } layer)
            return;

        for (var i = 0; _sprite.LayerMapTryGet(body, BuildLayerKey(layer, i), out var index, false); i++)
        {
            _sprite.LayerSetVisible(body, index, false);
        }
    }

    private Entity<SpriteComponent?>? GetBodySprite(EntityUid part)
    {
        if (_body.GetBody(part) is { } body && _spriteQuery.HasComp(body))
            return body;

        return _spriteQuery.HasComp(part) ? part : null;
    }

    private Enum? GetLayer(EntityUid part)
        => _visualQuery.CompOrNull(part)?.Layer;

    private List<SurgeryOverlay> GetNodeOverlays(OperatedComponent comp)
    {
        if (comp.GraphId is not { } id || !_proto.TryIndex(id, out var graph))
            return [];

        var node = graph.Nodes.Find(n => n.Name == comp.CurrentNode);
        return node?.Overlays ?? [];
    }

    private static string BuildLayerKey(Enum layer, int index) => $"{layer}{LayerSuffix}{index}";
}