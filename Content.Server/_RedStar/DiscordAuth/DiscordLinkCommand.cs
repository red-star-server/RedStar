using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server._RedStar.DiscordAuth;

[AnyCommand]
internal sealed partial class DiscordLinkCommand : LocalizedCommands
{
    [Dependency] private DiscordAuthManager _auth = default!;

    public override string Command => "discordlink";
    public override string Description => Loc.GetString("discord-auth-command-description");
    public override string Help => Loc.GetString("discord-auth-command-help");

    public override async void Execute(
        IConsoleShell shell,
        string argStr,
        string[] args)
    {
        if (shell.Player is not { } player)
        {
            shell.WriteError(Loc.GetString("shell-cannot-run-command-from-server"));
            return;
        }

        var result = await _auth.OpenLinkAsync(player);

        switch (result)
        {
            case DiscordAuthOpenResult.Opened:
                break;

            case DiscordAuthOpenResult.AlreadyLinked:
                shell.WriteLine(Loc.GetString("discord-auth-already-linked"));
                break;

            case DiscordAuthOpenResult.Disabled:
                shell.WriteError(Loc.GetString("discord-auth-disabled"));
                break;

            case DiscordAuthOpenResult.Failed:
                shell.WriteError(Loc.GetString("discord-auth-failed"));
                break;
        }
    }
}
