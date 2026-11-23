namespace Content.Shared._RedStar.Beekeeping.Data;

/// <summary>
/// What shows a bee trait. A trait that shows in nothing a colony does is learned by watching: it reveals itself after
/// the queen has worked in a hive for a while.
/// </summary>
public enum BeeTraitReveal : byte
{
    /// <summary>
    /// After the queen has worked in a hive for <c>BeeQueenComponent.ObserveTime</c>.
    /// </summary>
    Observe,

    /// <summary>
    /// A bee of the colony stings someone.
    /// </summary>
    Sting,

    /// <summary>
    /// A bee of the colony bites or stings someone holding it.
    /// </summary>
    Aggression,

    /// <summary>
    /// The hive lets angry bees out.
    /// </summary>
    Anger,

    /// <summary>
    /// A bee of the colony pollinates a plant by hand.
    /// </summary>
    Pollination,

    /// <summary>
    /// A worker's visit raises a plant's harvest.
    /// </summary>
    FlightPollination,

    /// <summary>
    /// The queen lays an egg.
    /// </summary>
    Egg,

    /// <summary>
    /// The hive keeps working in air it would otherwise stop in.
    /// </summary>
    HardAir,

    /// <summary>
    /// The hive stops in air it would otherwise work in.
    /// </summary>
    SoftAir,

    /// <summary>
    /// The colony swarms out of the hive.
    /// </summary>
    Swarm,
}
