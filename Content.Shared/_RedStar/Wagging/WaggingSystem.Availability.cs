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
        if (_net.IsClient)
            return;

        if (!TryFindAnimatedTail(args.Markings, "Tail", out var organ) &&
            !TryFindAnimatedTail(args.Markings, "Torso", out organ))
            return;

        EnsureWagging(ent.Owner, organ);
    }

    [SubscribeLocalEvent]
    private void OnOrganInserted(Entity<VisualOrganMarkingsComponent> ent, ref OrganGotInsertedEvent args)
    {
        if (_net.IsClient || !HasComp<VisualBodyComponent>(args.Target.Owner))
            return;

        if (Comp<OrganComponent>(ent).Category is not { } category || !HasAnimatedTail(ent.Comp.Markings))
            return;

        EnsureWagging(args.Target.Owner, category);
    }

    private void EnsureWagging(EntityUid body, ProtoId<OrganCategoryPrototype> organ)
    {
        var component = EnsureComp<WaggingComponent>(body);

        if (component.Organ != organ)
        {
            component.Organ = organ;
            DirtyField<WaggingComponent>(body, nameof(WaggingComponent.Organ));
        }

        if (component.ActionEntity is not null)
            return;

        _actions.AddAction(body, ref component.ActionEntity, component.Action, body);
        DirtyField<WaggingComponent>(body, nameof(WaggingComponent.ActionEntity));
    }

    private bool TryFindAnimatedTail(
        Dictionary<ProtoId<OrganCategoryPrototype>, Dictionary<HumanoidVisualLayers, List<Marking>>> markings,
        ProtoId<OrganCategoryPrototype> category,
        out ProtoId<OrganCategoryPrototype> organ)
    {
        organ = category;
        return markings.TryGetValue(category, out var sets) && HasAnimatedTail(sets);
    }

    private bool HasAnimatedTail(Dictionary<HumanoidVisualLayers, List<Marking>> markings)
    {
        return markings.TryGetValue(HumanoidVisualLayers.Tail, out var tails) &&
               tails.Any(tail => ProtoMan.HasIndex<MarkingPrototype>(tail.MarkingId.Id + "Animated"));
    }
}
