using System.Numerics;
using Content.Shared._RedStar.Xenobiology.Slimes;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Containers;
using Robust.Shared.Enums;

namespace Content.Client._RedStar.Xenobiology.Slimes;

/// <summary>
/// Renders each stomach occupant into a sprite layer without copying or spawning entities.
/// </summary>
public sealed partial class SlimeDigestionOverlay : Overlay
{
    [Dependency] private IClyde _clyde = default!;
    [Dependency] private ILogManager _logManager = default!;
    private readonly ISawmill _log;
    private readonly IEntityManager _entities;
    private readonly SpriteSystem _sprites;
    private readonly SharedContainerSystem _containers;
    private readonly Dictionary<EntityUid, IRenderTexture> _targets = new();
    private readonly HashSet<EntityUid> _unsupported = new();

    // Prepare textures before world sprites are drawn; their normal layers supply depth and lighting.
    public override OverlaySpace Space => OverlaySpace.WorldSpaceBelowEntities;

    public SlimeDigestionOverlay(IEntityManager entities)
    {
        IoCManager.InjectDependencies(this);
        _log = _logManager.GetSawmill("slime-digestion");
        _entities = entities;
        _sprites = entities.System<SpriteSystem>();
        _containers = entities.System<SharedContainerSystem>();
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        var query = _entities.EntityQueryEnumerator<SlimeDigestionVisualsComponent, SpriteComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var visuals, out var slimeSprite, out var transform))
        {
            if (transform.MapID != args.MapId ||
                !_sprites.LayerMapTryGet((uid, slimeSprite), SlimeVisualLayers.Digesting, out var layer, false))
                continue;

            if (!_containers.TryGetContainer(uid, SlimeDigestionVisualsComponent.ContainerId, out var container) ||
                container is not ContainerSlot { ContainedEntity: { } victim })
            {
                _sprites.LayerSetVisible((uid, slimeSprite), layer, false);
                Remove(uid);
                continue;
            }

            if (!_entities.TryGetComponent<SpriteComponent>(victim, out var sprite))
            {
                _sprites.LayerSetVisible((uid, slimeSprite), layer, false);
                if (_unsupported.Add(uid))
                    _log.Error($"Slime digestion cannot display {victim}: the contained entity has no SpriteComponent.");
                continue;
            }

            var bounds = new Box2Rotated(_sprites.GetLocalBounds((victim, sprite)), sprite.Rotation).CalcBoundingBox();
            if (bounds.Width <= 0 || bounds.Height <= 0)
                continue;

            if (!_targets.TryGetValue(uid, out var target))
            {
                target = _clyde.CreateRenderTarget((32, 32),
                    new RenderTargetFormatParameters(RenderTargetColorFormat.Rgba8Srgb), name: nameof(SlimeDigestionOverlay));
                _targets.Add(uid, target);
                _sprites.LayerSetTexture((uid, slimeSprite), layer, target.Texture);
                _sprites.LayerSetOffset((uid, slimeSprite), layer, visuals.InteriorOffset);
            }

            var scale = MathF.Min(visuals.InteriorSize.X / bounds.Width, visuals.InteriorSize.Y / bounds.Height);
            var center = bounds.Center + sprite.Offset;
            var position = new Vector2(16) + new Vector2(-center.X, center.Y) * scale * EyeManager.PixelsPerMeter;
            var handle = args.RenderHandle;
            handle.RenderInRenderTarget(target, () =>
                handle.DrawEntity(victim, position, new Vector2(scale), Angle.Zero, Angle.Zero,
                    Direction.South, sprite), Color.Transparent);
            _sprites.LayerSetVisible((uid, slimeSprite), layer, true);
        }
    }

    public void Remove(EntityUid uid)
    {
        _unsupported.Remove(uid);
        if (!_targets.Remove(uid, out var target))
            return;

        if (_entities.TryGetComponent<SpriteComponent>(uid, out var sprite) &&
            _sprites.LayerMapTryGet((uid, sprite), SlimeVisualLayers.Digesting, out var layer, false))
        {
            _sprites.LayerSetVisible((uid, sprite), layer, false);
            _sprites.LayerSetTexture((uid, sprite), layer, null);
        }
        target.Dispose();
    }

    protected override void DisposeBehavior()
    {
        foreach (var target in _targets.Values)
        {
            target.Dispose();
        }

        _targets.Clear();
        base.DisposeBehavior();
    }
}
