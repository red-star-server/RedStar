using Content.Server.NPC.HTN;
using Content.Server.NPC.Systems;
using Content.Shared.Damage.Systems;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Mobs;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Popups;
using Content.Shared.Tools.Systems;
using Content.Shared._RedStar.Beekeeping.Components;
using Content.Shared._RedStar.Beekeeping.Visuals;
using Robust.Shared.Configuration;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server._RedStar.Beekeeping.Systems;

/// <summary>
/// How a bee on an errand looks: which way it faces and how big it is while it dives.
/// </summary>
public sealed partial class BeeErrandSystem
{
    /// <summary>
    /// The sprite is a side view facing left: mirror it while the bee heads to the right of where it is, along its
    /// grid, since the camera turns with the grid the player stands on.
    /// </summary>
    private void Face(EntityUid bee, EntityCoordinates to)
    {
        var xform = Transform(bee);
        if (xform.GridUid is not { } grid)
            return;

        var from = _transform.WithEntityId(xform.Coordinates, grid).Position;
        var target = _transform.WithEntityId(to, grid).Position;
        SetFacing(bee, target - from);
    }

    public void SetFacing(EntityUid bee, System.Numerics.Vector2 localDirection)
    {
        if (localDirection.LengthSquared() < 0.0001f)
            return;

        _appearance.SetData(bee, BeeForagerVisuals.FacingRight, localDirection.X > 0f);
    }

    public void SetSize(EntityUid bee, BeeFlightSize size)
    {
        _appearance.SetData(bee, BeeForagerVisuals.Size, size);
    }

    public TimeSpan ShrinkTime(EntityUid bee)
    {
        var seconds = TryComp<BeeShrinkVisualsComponent>(bee, out var visuals)
            ? visuals.ShrinkTime
            : DefaultShrinkTime;
        return TimeSpan.FromSeconds(seconds);
    }
}
