using Content.Shared._Trauma.Common.DoAfter;
using Content.Shared.Body;

namespace Content.Shared._Trauma.DoAfter;

public sealed partial class DoAfterDelayMultiplierSystem : EntitySystem
{
    [Dependency] private BodySystem _body = default!;

    [SubscribeLocalEvent]
    private void OnBodyModify(EntityUid uid, BodyComponent component, ModifyDoAfterDelayEvent args)
    {
        _body.RelayEvent((uid, component), args);
    }

    [SubscribeLocalEvent]
    private void OnModify(Entity<DoAfterDelayMultiplierComponent> ent, ref BodyRelayedEvent<ModifyDoAfterDelayEvent> args)
    {
        args.Args.Multiplier *= ent.Comp.Multiplier;
    }
}
