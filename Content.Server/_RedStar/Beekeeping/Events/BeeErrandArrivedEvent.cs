using Content.Server._RedStar.Beekeeping.Data;

namespace Content.Server._RedStar.Beekeeping.Events;

/// <summary>
/// Raised on a bee on an errand (<see cref="BeeErrandComponent"/>) when it reaches its target on the way out. The system
/// of that kind of bee (a swarm, a meat colony's bee) does there what the errand is for and says what comes next. Not
/// handled: the bee visits the flower and turns back.
/// </summary>
[ByRefEvent]
public record struct BeeErrandArrivedEvent(EntityUid? Target, bool Handled = false, BeeErrandArrival Result = BeeErrandArrival.Visit);
