using System.Linq;
using System.Numerics;
using Content.Shared._RedStar.Beekeeping.Components;
using Content.Shared._RedStar.Beekeeping.Visuals;
using Robust.Client.GameObjects;
using Robust.Shared.Timing;

namespace Content.Client._RedStar.Beekeeping.Visuals;

/// <summary>
/// Wobbles the sprite of a buzzing bee around its real position along a loop that never quite repeats: two waves per
/// axis with different speeds. Each bee gets its own phase from its id, so a swarm does not move in step. A swarm
/// drawn as one sprite wobbles each of its bees (a group of layers) on its own, around where the prototype put it.
/// </summary>
public sealed partial class BeeBuzzVisualizerSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SpriteSystem _sprite = default!;
    [Dependency] private AppearanceSystem _appearance = default!;

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        var time = (float) _timing.RealTime.TotalSeconds;
        var query = EntityQueryEnumerator<BeeBuzzVisualsComponent, SpriteComponent>();
        while (query.MoveNext(out var uid, out var buzz, out var sprite))
        {
            var phase = uid.Id * 1.618f;
            var t = time * buzz.Speed * MathF.Tau;
            // A bee diving into a flower or the hive shrinks; its wobble shrinks with it, so it ends where it lands.
            var amplitude = buzz.Amplitude * Math.Min(1f, Math.Abs(sprite.Scale.X));
            if (buzz.WhileFlying &&
                !(_appearance.TryGetData<bool>(uid, BeeForagerVisuals.Flying, out var flying) && flying))
            {
                amplitude = 0f;
            }

            if (buzz.LayerGroup <= 0)
            {
                _sprite.SetOffset((uid, sprite), Wobble(t, phase) * amplitude);
                continue;
            }

            // The layers' own offsets are where each bee sits in the swarm; read them once, before any wobble.
            buzz.BaseOffsets ??= sprite.LayerData.Select(layer => layer.Offset).ToList();

            // The bees are side views facing left: the whole cloud is mirrored while it flies to the right.
            var right = _appearance.TryGetData<bool>(uid, BeeForagerVisuals.FacingRight, out var facing) && facing;
            var mirror = right ? -1f : 1f;
            for (var i = 0; i < buzz.BaseOffsets.Count; i++)
            {
                var bee = i / buzz.LayerGroup;
                var offset = Wobble(t, phase + bee * 2.39f) * amplitude;
                var baseOffset = buzz.BaseOffsets[i];
                _sprite.LayerSetOffset((uid, sprite), i, new Vector2(baseOffset.X * mirror, baseOffset.Y) + offset);
                _sprite.LayerSetScale((uid, sprite), i, new Vector2(mirror, 1f));
            }
        }
    }

    private static Vector2 Wobble(float t, float phase)
    {
        var x = MathF.Sin(t + phase) * 0.7f + MathF.Sin(t * 2.3f + phase * 1.7f) * 0.3f;
        var y = MathF.Sin(t * 1.3f + phase * 0.6f) * 0.7f + MathF.Cos(t * 2.9f + phase) * 0.3f;
        return new Vector2(x, y);
    }
}
