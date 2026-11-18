using System;
using System.Collections.Generic;
using Moderation.Utils;
using Rocket.API;
using Rocket.Unturned.Chat;
using Rocket.Unturned.Player;
using SDG.Unturned;

namespace Moderation.Commands
{
    public class CommandSpy : IRocketCommand
    {
        public AllowedCaller AllowedCaller => AllowedCaller.Player;
        public string Name => "spy";
        public string Help => "Takes a screenshot of the specified player.";
        public string Syntax => "<player>";
        public List<string> Aliases => new List<string>();
        public List<string> Permissions => new List<string> { "spy" };

        public void Execute(IRocketPlayer caller, string[] command)
        {
            var plugin = ModerationPlugin.Instance;
            UnturnedPlayer callerPlayer = (UnturnedPlayer)caller;

            if (command.Length < 1)
            {
                UnturnedChat.Say(caller, plugin.Translate("SpyFail"), plugin.MessageColor);
                return;
            }

            if (!plugin.CooldownService.CheckCooldown(callerPlayer.CSteamID.m_SteamID, "spy", out double remainingSeconds))
            {
                string formattedWait = DurationParser.FormatDuration((uint)remainingSeconds);
                UnturnedChat.Say(caller, plugin.Translate("SpyCooldown", formattedWait), plugin.MessageColor);
                return;
            }

            UnturnedPlayer targetPlayer = PlayerUtil.FindPlayer(command[0]);
            if (targetPlayer == null)
            {
                UnturnedChat.Say(caller, plugin.Translate("PlayerNotFound"), plugin.MessageColor);
                return;
            }

            targetPlayer.Player.sendScreenshot(callerPlayer.CSteamID, (steamId, data) =>
            {
                if (data != null && data.Length > 0)
                {
                    var placeholders = new Dictionary<string, string>
                    {
                        ["name"] = targetPlayer.CharacterName,
                        ["steamid"] = targetPlayer.CSteamID.m_SteamID.ToString(),
                        ["punisher_name"] = callerPlayer.CharacterName,
                        ["punisher_steamid"] = callerPlayer.CSteamID.m_SteamID.ToString(),
                        ["server_name"] = Provider.serverName,
                        ["server_icon"] = plugin.Configuration.Instance.Commands.MessageIconUrl,
                        ["avatar_url"] = plugin.Configuration.Instance.Commands.MessageIconUrl
                    };

                    plugin.DiscordWebhookService.SendWebhookList(
                        plugin.Configuration.Instance.DiscordService.SpyWebhookMessages,
                        placeholders,
                        data,
                        $"{targetPlayer.CSteamID.m_SteamID}_{DateTime.UtcNow.Ticks}.jpg"
                    );
                }
            });

            UnturnedChat.Say(caller, plugin.Translate("SpySuccess", targetPlayer.CharacterName), plugin.MessageColor);
        }
    }
}
