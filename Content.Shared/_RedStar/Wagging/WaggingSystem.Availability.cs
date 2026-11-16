using System.Linq;
using Content.Shared.Body;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;

namespace Content.Shared.Wagging;

public sealed partial class WaggingSystem
{
    [Dependency] private INetManager _net = default!;

    [SubscribeLocalEvent(after: [typeof(BodySystem)])]
    private void OnMarkingsApplied(Entity<VisualBodyComponent> ent, ref ApplyOrganMarkingsEvent args)
    {
        if (_net.IsClient || HasComp<WaggingComponent>(ent.Owner))
            return;

        if (!TryFindAnimatedTail(args, "Tail", out var organ) &&
            !TryFindAnimatedTail(args, "Torso", out organ))
            return;

        var component = EnsureComp<WaggingComponent>(ent.Owner);
        component.Organ = organ;
        if (component.ActionEntity is not null)
            return;

        _actions.AddAction(ent.Owner, ref component.ActionEntity, component.Action, ent.Owner);
        DirtyField<WaggingComponent>(ent.Owner, nameof(WaggingComponent.ActionEntity));
    }

    private bool TryFindAnimatedTail(
        ApplyOrganMarkingsEvent args,
        ProtoId<OrganCategoryPrototype> category,
        out ProtoId<OrganCategoryPrototype> organ)
    {
        organ = category;
        return args.Markings.TryGetValue(category, out var markings) &&
               markings.TryGetValue(HumanoidVisualLayers.Tail, out var tails) &&
               tails.Any(tail => ProtoMan.HasIndex<MarkingPrototype>(tail.MarkingId.Id + "Animated"));
    }
}
