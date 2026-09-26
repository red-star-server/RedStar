using Robust.Shared.Console;

namespace Content.Server._RedStar.DiscordAuth;

internal sealed partial class DiscordLinkCommand : LocalizedCommands
{
    [Dependency] private DiscordAuthManager _auth = default!;

    public override string Command => "discordlink";
    public override string Description => "Links your Discord account.";
    public override string Help => "discordlink";

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

        await _auth.OpenLinkAsync(player);
    }
}
