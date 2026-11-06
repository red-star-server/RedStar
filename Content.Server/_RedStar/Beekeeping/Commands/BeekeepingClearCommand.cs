using Content.Server.Administration;
using Content.Server._RedStar.Beekeeping.Components;
using Content.Shared.Administration;
using Content.Shared._RedStar.Beekeeping.Components;
using Robust.Shared.Console;

namespace Content.Server._RedStar.Beekeeping.Commands;

/// <summary>
/// Removes the bees that got out of hand on the admin's map: angry bees, angry queens, swarms, bees on errands and wild
/// hives. Hives built by players and the bees in their hands are left alone. With "all", every map.
/// </summary>
[AdminCommand(AdminFlags.Admin)]
public sealed partial class BeekeepingClearCommand : LocalizedEntityCommands
{
    [Dependency] private SharedTransformSystem _transform = default!;

    public override string Command => "beekeeping_clear";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var all = args.Length > 0 && args[0] == "all";
        EntityUid? map = null;
        if (!all)
        {
            if (shell.Player?.AttachedEntity is not { } player)
            {
                shell.WriteError(Loc.GetString("cmd-beekeeping_clear-no-map"));
                return;
            }

            map = _transform.GetMap(player);
        }

        var removed = 0;
        removed += Clear<BeeSwarmComponent>(map);

        // Bees out on errands are in the air for a short while anyway; whatever is out now goes.
        removed += Clear<BeeErrandComponent>(map);

        var hives = EntityManager.EntityQueryEnumerator<BeehiveComponent, TransformComponent>();
        while (hives.MoveNext(out var uid, out var hive, out var xform))
        {
            if (hive.Wild && (map == null || xform.MapUid == map))
            {
                EntityManager.QueueDeleteEntity(uid);
                removed++;
            }
        }

        shell.WriteLine(Loc.GetString("cmd-beekeeping_clear-done", ("count", removed)));
    }

    private int Clear<T>(EntityUid? map) where T : IComponent
    {
        var count = 0;
        var query = EntityManager.EntityQueryEnumerator<T, TransformComponent>();
        while (query.MoveNext(out var uid, out _, out var xform))
        {
            if (map != null && xform.MapUid != map)
                continue;

            EntityManager.QueueDeleteEntity(uid);
            count++;
        }

        return count;
    }
}
