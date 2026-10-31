using System.Linq;
using Content.Shared._Trauma.Common.Body;
using Content.Shared.Body;
using Content.Shared.Gibbing;
using Robust.Shared.Containers;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Shared._Trauma.Body;

/// <summary>
/// System that handles bodypart logic and provides API for working with them.
/// </summary>
public sealed partial class BodyPartSystem : CommonBodyPartSystem
{
    [Dependency] private BodySystem _body = default!;
    [Dependency] private BodyCacheSystem _cache = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private EntityQuery<BodyPartComponent> _query;
    [Dependency] private EntityQuery<ChildOrganComponent> _childQuery;
    [Dependency] private EntityQuery<OrganComponent> _organQuery;

    [SubscribeLocalEvent]
    private void OnPartInserted(Entity<BodyPartComponent> ent, ref OrganGotInsertedEvent args)
    {
        if (_timing.ApplyingState)
            return;

        // fresh part, no organs inside
        if (GetSeveredOrgansContainer(ent.AsNullable()) is not {} container)
            return;

        var body = args.Target.AsNullable();
        var organs = new List<EntityUid>(container.ContainedEntities); // no CME
        foreach (var organ in organs)
        {
            if (_body.InsertOrgan(body, organ))
                continue;

            Log.Error($"Couldn't insert {ToPrettyString(organ)} from {ToPrettyString(ent)} back into {ToPrettyString(args.Target)} after being attached, ejecting it");
            if (!_container.Remove(organ, container))
                Log.Error($"Organ {ToPrettyString(organ)} got stuck inside of {ToPrettyString(ent)} after being inserted into {ToPrettyString(args.Target)}");
        }
    }

    [SubscribeLocalEvent]
    private void OnPartRemoved(Entity<BodyPartComponent> ent, ref OrganGotRemovedEvent args)
    {
        // don't transfer parts if the body is being deleted
        // note that this will still transfer if the part is being deleted, so its organs will go away too
        if (TerminatingOrDeleted(args.Target) || _timing.ApplyingState)
            return;

        var body = args.Target.AsNullable();
        if (TerminatingOrDeleted(ent))
        {
            // this part is being deleted so detach the children
            foreach (var organ in ent.Comp.Children.Values.ToArray())
            {
                _body.RemoveOrgan(body, organ);
            }
            return;
        }

        var container = EnsureSeveredOrgansContainer(ent);
        foreach (var (category, organ) in ent.Comp.Children.ToArray())
        {
            // slot has an organ so try to put it in the container
            if (!_container.Insert(organ, container))
            {
                // probably from failing to be removed, suspicious
                Log.Error($"Failed to store {ToPrettyString(ent)}'s {category} organ {ToPrettyString(organ)}!");
                continue;
            }
        }
    }

    [SubscribeLocalEvent]
    private void OnBeingGibbed(Entity<BodyPartComponent> ent, ref BeingGibbedEvent args)
    {
        if (GetSeveredOrgansContainer(ent.AsNullable()) is not {} container)
            return;

        // gibbing a severed head spills its brains out >:D
        foreach (var organ in container.ContainedEntities)
        {
            args.Giblets.Add(organ);
        }
    }

    internal void OrganInserted(Entity<BodyPartComponent?> part, Entity<OrganComponent?> organ)
    {
        DebugTools.Assert(part.Owner != organ.Owner);
        if (!_query.Resolve(part, ref part.Comp) ||
            _body.GetCategory(organ) is not {} category ||
            !CanInsertOrgan(part, category)) // just incase
            return;

        part.Comp.Children[category] = organ;
        DirtyField(part, part.Comp, nameof(BodyPartComponent.Children));

        var ev = new OrganInsertedIntoPartEvent(organ, category);
        RaiseLocalEvent(part, ref ev);
    }

    internal void OrganRemoved(Entity<BodyPartComponent?> part, Entity<OrganComponent?> organ)
    {
        DebugTools.Assert(part.Owner != organ.Owner);
        if (!_query.Resolve(part, ref part.Comp) ||
            _body.GetCategory(organ) is not {} category)
            return;

        part.Comp.Children.Remove(category);
        DirtyField(part, part.Comp, nameof(BodyPartComponent.Children));

        var ev = new OrganRemovedFromPartEvent(organ, category);
        RaiseLocalEvent(part, ref ev);
    }
}
