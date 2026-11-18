using System.Collections.Generic;
using Moderation.Utils;
using Rocket.API;
using Rocket.Unturned.Chat;
using Rocket.Unturned.Player;

namespace Moderation.Commands
{
    public class CommandBans : IRocketCommand
    {
        public AllowedCaller AllowedCaller => AllowedCaller.Both;
        public string Name => "bans";
        public string Help => "Displays a list of all or player bans.";
        public string Syntax => "[player]";
        public List<string> Aliases => new List<string>();
        public List<string> Permissions => new List<string> { "bans" };

        public void Execute(IRocketPlayer caller, string[] command)
        {
            var plugin = ModerationPlugin.Instance;
            var allBans = plugin.DatabaseService.GetActiveBans();

            if (command.Length == 0)
            {
                if (caller is UnturnedPlayer)
                {
                    UnturnedChat.Say(caller, plugin.Translate("BansLimit"), plugin.MessageColor);
                    return;
                }

                if (allBans.Count == 0)
                {
                    UnturnedChat.Say(caller, plugin.Translate("BansNone"), plugin.MessageColor);
                    return;
                }

                foreach (var ban in allBans)
                {
                    UnturnedChat.Say(caller, plugin.Translate("BansLine", ban.Id, ban.SteamName ?? ban.SteamID.ToString(), ban.SteamID, ban.PunisherName, ban.PunisherSteamID), plugin.MessageColor);
                }
            }
            else
            {
                if (!PlayerUtil.TryParseSteamID(command[0], out ulong targetSteamId))
                {
                    UnturnedChat.Say(caller, plugin.Translate("PlayerNotFound"), plugin.MessageColor);
                    return;
                }

                var playerBans = allBans.FindAll(b => b.SteamID == targetSteamId);
                if (playerBans.Count == 0)
                {
                    UnturnedChat.Say(caller, plugin.Translate("BansNone"), plugin.MessageColor);
                    return;
                }

                foreach (var ban in playerBans)
                {
                    UnturnedChat.Say(caller, plugin.Translate("BansLine", ban.Id, ban.SteamName ?? ban.SteamID.ToString(), ban.SteamID, ban.PunisherName, ban.PunisherSteamID), plugin.MessageColor);
                }
            }
        }
    }
}
