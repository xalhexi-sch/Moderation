using System.Collections.Generic;
using Moderation.Models;
using Moderation.Utils;
using Rocket.API;
using Rocket.Unturned.Chat;
using Rocket.Unturned.Player;

namespace Moderation.Commands
{
    public class CommandPlayerinfo : IRocketCommand
    {
        public AllowedCaller AllowedCaller => AllowedCaller.Both;
        public string Name => "playerinfo";
        public string Help => "Displays player name, country and bans, kicks, warns count.";
        public string Syntax => "<player>";
        public List<string> Aliases => new List<string>();
        public List<string> Permissions => new List<string> { "playerinfo" };

        public void Execute(IRocketPlayer caller, string[] command)
        {
            var plugin = ModerationPlugin.Instance;
            if (command.Length < 1)
            {
                UnturnedChat.Say(caller, plugin.Translate("PlayerInfoInvalid"), plugin.MessageColor);
                return;
            }

            ulong steamId = 0;
            string playerName = command[0];
            string country = "Unknown";

            UnturnedPlayer online = PlayerUtil.FindPlayer(command[0]);
            if (online != null)
            {
                steamId = online.CSteamID.m_SteamID;
                playerName = online.CharacterName;
            }
            else if (!PlayerUtil.TryParseSteamID(command[0], out steamId))
            {
                steamId = plugin.DatabaseService?.FindSteamIdByName(command[0]) ?? 0;
            }

            if (steamId == 0)
            {
                UnturnedChat.Say(caller, plugin.Translate("PlayerNotFound"), plugin.MessageColor);
                return;
            }

            PlayerRecord record = plugin.DatabaseService.GetPlayer(steamId);
            if (record != null)
            {
                if (!string.IsNullOrEmpty(record.SteamName))
                    playerName = record.SteamName;
                if (!string.IsNullOrEmpty(record.Country))
                    country = record.Country;
            }

            plugin.DatabaseService.GetPunishmentCounts(steamId, out int bans, out int kicks, out int warns);

            UnturnedChat.Say(caller, plugin.Translate("PlayerInfoMessage", playerName, country, bans, kicks, warns), plugin.MessageColor);
        }
    }
}
