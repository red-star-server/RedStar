using Content.Server.Administration;
using Content.Server.Database;
using Content.Shared._RedStar.Sponsors;
using Content.Shared.Administration;
using Robust.Shared.Console;
using Robust.Shared.Prototypes;

namespace Content.Server._RedStar.Sponsors.Commands;

[AdminCommand(AdminFlags.Host)]
internal sealed partial class SponsorSetCommand : LocalizedCommands
{
    [Dependency] private IServerDbManager _db = default!;
    [Dependency] private IPrototypeManager _prototypes = default!;
    [Dependency] private ServerSponsorManager _sponsors = default!;
    [Dependency] private IEntityManager _entities = default!;

    public override string Command => "sponsor:set";
    public override string Description => "Sets a player's sponsor tier.";
    public override string Help => "sponsor:set <username> <tier>";

    public override async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 2)
        {
            shell.WriteError(Help);
            return;
        }

        if (!_prototypes.HasIndex<SponsorTierPrototype>(args[1]))
        {
            shell.WriteError($"Unknown sponsor tier '{args[1]}'.");
            return;
        }

        var player = await _db.GetPlayerRecordByUserName(args[0]);
        if (player == null)
        {
            shell.WriteError($"Player '{args[0]}' was never seen on this server.");
            return;
        }

        await _sponsors.SetTierAsync(player.UserId, args[1]);
        await _entities.System<SponsorSystem>().SyncPlayerAsync(player.UserId);
        shell.WriteLine($"Set '{player.LastSeenUserName}' sponsor tier to '{args[1]}'.");
    }
}

[AdminCommand(AdminFlags.Host)]
internal sealed partial class SponsorRemoveCommand : LocalizedCommands
{
    [Dependency] private IServerDbManager _db = default!;
    [Dependency] private ServerSponsorManager _sponsors = default!;
    [Dependency] private IEntityManager _entities = default!;

    public override string Command => "sponsor:remove";
    public override string Description => "Removes a player's sponsor status.";
    public override string Help => "sponsor:remove <username>";

    public override async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 1)
        {
            shell.WriteError(Help);
            return;
        }

        var player = await _db.GetPlayerRecordByUserName(args[0]);
        if (player == null)
        {
            shell.WriteError($"Player '{args[0]}' was never seen on this server.");
            return;
        }

        await _sponsors.RemoveAsync(player.UserId);
        await _entities.System<SponsorSystem>().SyncPlayerAsync(player.UserId);
        shell.WriteLine($"Removed sponsor status from '{player.LastSeenUserName}'.");
    }
}

[AnyCommand]
internal sealed partial class SponsorOocColorCommand : LocalizedCommands
{
    [Dependency] private ServerSponsorManager _sponsors = default!;
    [Dependency] private IEntityManager _entities = default!;

    public override string Command => "sponsor:ooc";
    public override string Description => "Sets your sponsor OOC name color.";
    public override string Help => "sponsor:ooc <#RRGGBB|reset>";

    public override async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (shell.Player is not { } player)
        {
            shell.WriteError(Loc.GetString("shell-cannot-run-command-from-server"));
            return;
        }

        if (args.Length != 1)
        {
            shell.WriteError(Help);
            return;
        }

        Color? color = null;
        if (!args[0].Equals("reset", StringComparison.OrdinalIgnoreCase))
        {
            if (!Color.TryFromHex(args[0], out var parsed))
            {
                shell.WriteError(Loc.GetString("shell-invalid-color-hex"));
                return;
            }

            color = parsed;
        }

        if (!await _sponsors.SetOocColorAsync(player.UserId, color))
        {
            shell.WriteError(Loc.GetString("sponsor-color-not-available"));
            return;
        }

        await _entities.System<SponsorSystem>().SyncPlayerAsync(player.UserId);
        shell.WriteLine(Loc.GetString(color == null ? "sponsor-color-reset" : "sponsor-color-updated"));
    }
}

[AnyCommand]
internal sealed partial class SponsorGhostColorCommand : LocalizedCommands
{
    [Dependency] private ServerSponsorManager _sponsors = default!;
    [Dependency] private IEntityManager _entities = default!;

    public override string Command => "sponsor:ghost";
    public override string Description => "Sets your sponsor ghost color.";
    public override string Help => "sponsor:ghost <#RRGGBB|reset>";

    public override async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (shell.Player is not { } player)
        {
            shell.WriteError(Loc.GetString("shell-cannot-run-command-from-server"));
            return;
        }

        if (args.Length != 1)
        {
            shell.WriteError(Help);
            return;
        }

        Color? color = null;
        if (!args[0].Equals("reset", StringComparison.OrdinalIgnoreCase))
        {
            if (!Color.TryFromHex(args[0], out var parsed))
            {
                shell.WriteError(Loc.GetString("shell-invalid-color-hex"));
                return;
            }

            color = parsed;
        }

        if (!await _sponsors.SetGhostColorAsync(player.UserId, color))
        {
            shell.WriteError(Loc.GetString("sponsor-color-not-available"));
            return;
        }

        await _entities.System<SponsorSystem>().SyncPlayerAsync(player.UserId);
        shell.WriteLine(Loc.GetString(color == null ? "sponsor-color-reset" : "sponsor-color-updated"));
    }
}
