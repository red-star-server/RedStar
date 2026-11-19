using Content.Shared._RedStar.Beekeeping.Components;
using Content.Shared._RedStar.Beekeeping.Systems;
using Robust.Client.GameObjects;
using Robust.Shared.Containers;

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

    // Visibility is not networked, so a catch rolled back by prediction must show the flight again itself.
    [SubscribeLocalEvent]
    private void OnQueenReturned(Entity<BeeCatchableQueenComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        if (TryComp<SpriteComponent>(ent, out var sprite))
            _sprite.SetVisible((ent, sprite), true);
    }
}
