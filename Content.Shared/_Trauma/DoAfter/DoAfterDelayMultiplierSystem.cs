using Content.Shared._Trauma.Common.DoAfter;
using Content.Shared.Body;

namespace Content.Shared._Trauma.DoAfter;

public sealed partial class DoAfterDelayMultiplierSystem : EntitySystem
{
    [Dependency] private BodySystem _body = default!;

    [SubscribeLocalEvent]
    private void OnBodyModify(Entity<BodyComponent> ent, ref ModifyDoAfterDelayEvent args)
    {
        _body.RelayEvent(ent, ref args);
    }

    [SubscribeLocalEvent]
    private void OnModify(Entity<DoAfterDelayMultiplierComponent> ent, ref BodyRelayedEvent<ModifyDoAfterDelayEvent> args)
    {
        var ev = args.Args;
        ev.Multiplier *= ent.Comp.Multiplier;
        args.Args = ev;
    }
}
