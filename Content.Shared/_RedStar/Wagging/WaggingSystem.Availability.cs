using System.Linq;
using Content.Shared.Body;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Robust.Shared.Network;

namespace Content.Shared.Wagging;

public sealed partial class WaggingSystem
{
    [Dependency] private INetManager _net = default!;

    [SubscribeLocalEvent(after: [typeof(BodySystem)])]
    private void OnMarkingsApplied(Entity<VisualBodyComponent> ent, ref ApplyOrganMarkingsEvent args)
    {
        if (_net.IsClient || HasComp<WaggingComponent>(ent.Owner) ||
            !args.Markings.TryGetValue("Torso", out var markings) ||
            !markings.TryGetValue(HumanoidVisualLayers.Tail, out var tails) ||
            !tails.Any(tail => ProtoMan.HasIndex<MarkingPrototype>(tail.MarkingId.Id + "Animated")))
            return;

        var component = EnsureComp<WaggingComponent>(ent.Owner);
        if (component.ActionEntity is not null)
            return;

        _actions.AddAction(ent.Owner, ref component.ActionEntity, component.Action, ent.Owner);
        DirtyField<WaggingComponent>(ent.Owner, nameof(WaggingComponent.ActionEntity));
    }
}
