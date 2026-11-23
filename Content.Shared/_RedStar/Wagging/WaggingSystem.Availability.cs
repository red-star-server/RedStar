using Content.Shared.Body;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;

namespace Content.Shared.Wagging;

public sealed partial class WaggingSystem
{
    [Dependency] private INetManager _net = default!;

    private const string AnimatedSuffix = "Animated";

    [SubscribeLocalEvent(after: [typeof(BodySystem)])]
    private void OnMarkingsApplied(Entity<VisualBodyComponent> ent, ref ApplyOrganMarkingsEvent args)
    {
        if (!_net.IsClient)
            SyncWagging(ent);
    }

    [SubscribeLocalEvent]
    private void OnOrganInserted(Entity<VisualBodyComponent> ent, ref OrganInsertedIntoEvent args)
    {
        if (!_net.IsClient)
            SyncWagging(ent);
    }

    [SubscribeLocalEvent]
    private void OnOrganRemoved(Entity<VisualBodyComponent> ent, ref OrganRemovedFromEvent args)
    {
        if (!_net.IsClient)
            SyncWagging(ent);
    }

    private void SyncWagging(Entity<VisualBodyComponent> ent)
    {
        if (!_visualBody.TryGatherMarkingsData(ent.AsNullable(), [HumanoidVisualLayers.Tail], out _, out _, out var applied))
            return;

        if (TryGetTailAnimation(applied, "Tail", out var animated))
        {
            EnsureWagging(ent.Owner, "Tail", animated);
            return;
        }

        if (TryGetTailAnimation(applied, "Torso", out animated))
        {
            EnsureWagging(ent.Owner, "Torso", animated);
            return;
        }

        if (!TryComp<WaggingComponent>(ent.Owner, out var comp) || comp.ActionEntity is not { } action)
            return;

        _actions.RemoveAction(ent.Owner, action);
        comp.ActionEntity = null;
        DirtyField<WaggingComponent>(ent.Owner, nameof(WaggingComponent.ActionEntity));
    }

    private void EnsureWagging(EntityUid body, ProtoId<OrganCategoryPrototype> organ, bool animated)
    {
        var component = EnsureComp<WaggingComponent>(body);

        if (component.Organ != organ)
        {
            component.Organ = organ;
            DirtyField<WaggingComponent>(body, nameof(WaggingComponent.Organ));
        }

        if (component.Wagging != animated)
        {
            component.Wagging = animated;
            DirtyField<WaggingComponent>(body, nameof(WaggingComponent.Wagging));
        }

        if (component.ActionEntity is not null)
            return;

        _actions.AddAction(body, ref component.ActionEntity, component.Action, body);
        DirtyField<WaggingComponent>(body, nameof(WaggingComponent.ActionEntity));
    }

    private bool TryGetTailAnimation(
        Dictionary<ProtoId<OrganCategoryPrototype>, Dictionary<HumanoidVisualLayers, List<Marking>>> applied,
        ProtoId<OrganCategoryPrototype> category,
        out bool animated)
    {
        animated = false;

        if (!applied.TryGetValue(category, out var sets) ||
            !sets.TryGetValue(HumanoidVisualLayers.Tail, out var tails))
            return false;

        var found = false;
        foreach (var tail in tails)
        {
            var id = tail.MarkingId.Id;
            if (ProtoMan.HasIndex<MarkingPrototype>(id + AnimatedSuffix))
            {
                found = true;
            }
            else if (id.EndsWith(AnimatedSuffix) && ProtoMan.HasIndex<MarkingPrototype>(id[..^AnimatedSuffix.Length]))
            {
                found = true;
                animated = true;
            }
        }

        return found;
    }
}
