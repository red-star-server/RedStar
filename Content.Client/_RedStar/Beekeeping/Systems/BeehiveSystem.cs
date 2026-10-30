using Content.Shared._RedStar.Beekeeping.Systems;
using Robust.Client.GameObjects;

namespace Content.Client._RedStar.Beekeeping.Systems;

/// <summary>
/// Client side of beekeeping: the shared examine text and predictions. The sprites are driven by GenericVisualizer.
/// </summary>
public sealed partial class BeehiveSystem : SharedBeehiveSystem
{
    [Dependency] private SpriteSystem _sprite = default!;

    protected override void OnQueenCaught(EntityUid flyer)
    {
        // The queen is already in the hand; hide the flight until the server removes it.
        if (TryComp<SpriteComponent>(flyer, out var sprite))
            _sprite.SetVisible((flyer, sprite), false);
    }
}
