using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared._RedStar.Beekeeping.Components;

namespace Content.Shared._RedStar.Beekeeping.Systems;

/// <summary>
/// Catching a lone flying queen by hand, predicted on the client.
/// </summary>
public abstract partial class SharedBeehiveSystem
{
    [Dependency] private SharedHandsSystem _catchHands = default!;

    [SubscribeLocalEvent]
    private void OnCatchQueen(Entity<BeeCatchableQueenComponent> ent, ref InteractHandEvent args)
    {
        if (args.Handled ||
            !_containers.TryGetContainer(ent, ent.Comp.Container, out var container) ||
            container.ContainedEntities.Count == 0)
        {
            return;
        }

        args.Handled = true;
        var queen = container.ContainedEntities[0];
        _containers.Remove(queen, container, force: true);
        // Straight into the hand: no flying-to-hand animation on top of the catch.
        _catchHands.PickupOrDrop(args.User, queen, animate: false);
        OnQueenCaught(ent);
    }

    /// <summary>
    /// The flight is over once she is caught: the server removes the flyer, the client hides it until then.
    /// </summary>
    protected virtual void OnQueenCaught(EntityUid flyer)
    {
    }
}
