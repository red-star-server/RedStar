using Content.Server._RedStar.Beekeeping.Data;
using Robust.Shared.Audio;

namespace Content.Server._RedStar.Beekeeping.Components;

/// <summary>
/// A bee or queen left on the floor (dropped, or landed after a throw) flies straight back into its own hive:
/// a bee item to the hive it was taken from, a queen to the hive she was last taken out of. It only flies while
/// the hive is on the same grid, close enough and in clear sight; otherwise it stays where it is and tries
/// again later. Picking it up stops the flight.
/// </summary>
[RegisterComponent]
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
    /// A queen let go with no hive to go back to flies off at once to found her own, like a swarm. When there is
    /// nowhere to go she tries again this often.
    /// </summary>
    [DataField]
    public TimeSpan NestRetry = TimeSpan.FromSeconds(5);

    [ViewVariables]
    public TimeSpan NextNestTry;

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

    [ViewVariables]
    public TimeSpan NextCheck;
}
