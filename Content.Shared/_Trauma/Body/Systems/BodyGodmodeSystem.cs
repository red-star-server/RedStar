using Content.Shared._Trauma.Common.Body;
using Content.Shared.Body;
using Content.Shared.Damage.Components;

namespace Content.Shared._Trauma.Body;

/// <summary>
/// Propogates godmode to all organs and prevents removing them.
/// </summary>
public sealed partial class BodyGodmodeSystem : EntitySystem
{
    [Dependency] private EntityQuery<BodyComponent> _bodyQuery = default!;

    [SubscribeLocalEvent]
    private void OnStartup(Entity<GodmodeComponent> ent, ref ComponentStartup args)
    {
        if (_bodyQuery.CompOrNull(ent)?.Organs is not {} organs)
            return;

        foreach (var organ in organs.ContainedEntities)
        {
            EnsureComp<GodmodeComponent>(organ);
        }
    }

    [SubscribeLocalEvent]
    private void OnShutdown(Entity<GodmodeComponent> ent, ref ComponentShutdown args)
    {
        if (_bodyQuery.CompOrNull(ent)?.Organs is not {} organs)
            return;

        foreach (var organ in organs.ContainedEntities)
        {
            RemComp<GodmodeComponent>(organ);
        }
    }

    [SubscribeLocalEvent]
    private void OnRemoveAttempt(Entity<GodmodeComponent> ent, ref OrganRemoveAttemptEvent args)
    {
        // no stealing godmode limbs for yourself, and doesn't make sense that you can have organs harvested etc with godmode
        args.Cancelled = true;
    }
}
