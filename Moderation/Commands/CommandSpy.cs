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

            if (callerPlayer == null) return;

            if (command.Length < 1)
            {
                UnturnedChat.Say(caller, plugin.Translate("SpyFail"), plugin.MessageColor);
                return;
            }

            if (plugin?.CooldownService != null && !plugin.CooldownService.CheckCooldown(callerPlayer.CSteamID.m_SteamID, "spy", out double remainingSeconds))
            {
                string formattedWait = DurationParser.FormatDuration((uint)remainingSeconds);
                UnturnedChat.Say(caller, plugin.Translate("SpyCooldown", formattedWait), plugin.MessageColor);
                return;
            }

            UnturnedPlayer targetPlayer = PlayerUtil.FindPlayer(command[0]);
            if (targetPlayer?.Player == null)
            {
                UnturnedChat.Say(caller, plugin.Translate("PlayerNotFound"), plugin.MessageColor);
                return;
            }

            try
            {
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
                            ["server_name"] = Provider.serverName ?? "Unturned Server",
                            ["server_icon"] = plugin?.Configuration?.Instance?.Commands?.MessageIconUrl ?? string.Empty,
                            ["avatar_url"] = plugin?.Configuration?.Instance?.Commands?.MessageIconUrl ?? string.Empty
                        };

                        var webhooks = plugin?.Configuration?.Instance?.DiscordService?.SpyWebhookMessages;
                        if (webhooks != null && plugin?.DiscordWebhookService != null)
                        {
                            plugin.DiscordWebhookService.SendWebhookList(
                                webhooks,
                                placeholders,
                                data,
                                $"{targetPlayer.CSteamID.m_SteamID}_{DateTime.UtcNow.Ticks}.jpg"
                            );
                        }
                    }
                });

                UnturnedChat.Say(caller, plugin.Translate("SpySuccess", targetPlayer.CharacterName), plugin.MessageColor);
            }
            catch (Exception ex)
            {
                UnturnedChat.Say(caller, $"Error executing spy: {ex.Message}", plugin.MessageColor);
            }
        }
    }
}
