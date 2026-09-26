using Robust.Shared.Configuration;

namespace Content.Shared._RedStar.DiscordAuth;

[CVarDefs]
public sealed class DiscordAuthCVars
{
    public static readonly CVarDef<bool> Enabled =
        CVarDef.Create(
            "discord.auth.enabled",
            false,
            CVar.SERVERONLY);

    public static readonly CVarDef<string> ApiUrl =
        CVarDef.Create(
            "discord.auth.api_url",
            "",
            CVar.CONFIDENTIAL | CVar.SERVERONLY);

    public static readonly CVarDef<string> ApiKey =
        CVarDef.Create(
            "discord.auth.api_key",
            "",
            CVar.CONFIDENTIAL | CVar.SERVERONLY);
}
