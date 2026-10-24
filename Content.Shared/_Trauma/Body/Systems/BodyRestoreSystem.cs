using Robust.Shared.Prototypes;

using Content.Shared.Body;
using Content.Shared.Body.Systems;
using Content.Shared.Damage.Systems;
using Content.Shared.Rejuvenate;

namespace Content.Shared._Trauma.Body;

public sealed partial class BodyRestoreSystem : EntitySystem
{
    [Dependency] private BodySystem _body = default!;
    [Dependency] private BodyPartSystem _part = default!;

    [SubscribeLocalEvent(before: new[] { typeof(DamageableSystem), typeof(BloodstreamSystem) })]
    private void OnRejuvenate(Entity<BodyComponent> ent, ref RejuvenateEvent args)
    {
        if (_part.GetRootPart(ent.AsNullable()) == null)
            return;

        RestoreBody(ent.AsNullable());
        // not using _body.RelayEvent because it wraps it in BodyRelayedEvent
        foreach (var organ in _body.GetOrgans(ent.AsNullable()))
        {
            RaiseLocalEvent(organ, args); // TODO: make by ref if it stops being a class
        }
    }

    public void RestoreBody(Entity<BodyComponent?> body)
    {
        if (_part.GetRootPart(body) is {} root)
            _part.RestoreInitialOrgans(root.AsNullable()); // recursively restore the body from its prototype
        else
            Log.Error($"Tried to restore body {ToPrettyString(body)} which had no root part!");
    }
}
