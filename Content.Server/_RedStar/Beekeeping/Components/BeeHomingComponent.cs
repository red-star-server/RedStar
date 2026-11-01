using Content.Server._RedStar.Beekeeping.Data;
using Robust.Shared.Audio;

namespace Content.Server._RedStar.Beekeeping.Components;

/// <summary>
/// A queen left on the floor (dropped, or landed after a throw) flies straight back into the hive she was last
/// taken out of, or into any empty one in sight. It only flies while the hive is on the same grid, close enough, in
/// clear sight and willing to take her; otherwise she stays where she is and tries again later. Picking her up stops
/// the flight. Worker bees and the scout are not handled here: they fly home as NPCs
/// (<c>BeeErrandSystem.FlyHome</c>).
/// </summary>
[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class BeeHomingComponent : Component
{
    /// <summary>
    /// Flight speed in tiles per second.
    /// </summary>
    [DataField]
    public float Speed = 2f;

    /// <summary>
    /// Farthest distance in tiles from which it still flies home.
    /// </summary>
    [DataField]
    public float Range = 12f;

    /// <summary>
    /// How often it checks whether it can fly home, and whether the way is still clear while flying.
    /// </summary>
    [DataField]
    public TimeSpan CheckInterval = TimeSpan.FromSeconds(1);

    /// <summary>
    /// The hive it is flying to right now.
    /// </summary>
    [ViewVariables]
    public EntityUid? Target;

    /// <summary>
    /// How it turns towards the hive: a side-view sprite is mirrored, a top-down one is rotated or left alone.
    /// </summary>
    [DataField]
    public BeeHomingFacing Facing = BeeHomingFacing.Mirror;

    /// <summary>
    /// Buzz played while it is free: flying home or sitting on the floor. Null keeps the ambient sound as it is.
    /// </summary>
    [DataField]
    public SoundSpecifier? FlyingSound = new SoundPathSpecifier("/Audio/_RedStar/Ambience/Beekeeping/bee_flying.ogg");

    /// <summary>
    /// The ambient sound from the prototype, played while it is held or packed away.
    /// </summary>
    [ViewVariables]
    public SoundSpecifier? RestingSound;

    /// <summary>
    /// Whether the free buzz is playing now.
    /// </summary>
    [ViewVariables]
    public bool FreeSound;

    [ViewVariables]
    public bool Flying;

    [ViewVariables, AutoPausedField]
    public TimeSpan NextCheck;
}
