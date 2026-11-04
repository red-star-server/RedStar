using Content.Shared._Trauma.Common.Body;
using Content.Shared.Body;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;

namespace Content.Shared._Trauma.Body;

/// <summary>
/// Propogates godmode to all organs and prevents removing them.
/// </summary>
public sealed partial class BodyGodmodeSystem : EntitySystem
{
    [Dependency] private EntityQuery<BodyComponent> _bodyQuery;
    [Dependency] private SharedGodmodeSystem _godmode = default!;

    [SubscribeLocalEvent]
    private void OnStartup(Entity<GodmodeComponent> ent, ref ComponentStartup args)
    {
        if (_bodyQuery.CompOrNull(ent)?.Organs is not {} organs)
            return;

        foreach (var organ in organs.ContainedEntities)
        {
            _godmode.EnableGodmode(organ);
        }
    }

    [SubscribeLocalEvent]
    private void OnShutdown(Entity<GodmodeComponent> ent, ref ComponentShutdown args)
    {
        if (_bodyQuery.CompOrNull(ent)?.Organs is not {} organs)
            return;

        foreach (var organ in organs.ContainedEntities)
        {
            _godmode.DisableGodmode(organ);
        }
    }

    [SubscribeLocalEvent]
    private void OnOrganInserted(Entity<GodmodeComponent> ent, ref OrganInsertedIntoEvent args)
    {
        _godmode.EnableGodmode(args.Organ);
    }

    [SubscribeLocalEvent]
    private void OnRemoveAttempt(Entity<GodmodeComponent> ent, ref OrganRemoveAttemptEvent args)
    {
        // no stealing godmode limbs for yourself, and doesn't make sense that you can have organs harvested etc with godmode
        args.Cancelled = true;
    }
}
