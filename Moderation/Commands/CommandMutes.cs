using System.Collections.Generic;
using Moderation.Utils;
using Rocket.API;
using Rocket.Unturned.Chat;
using Rocket.Unturned.Player;

namespace Moderation.Commands
{
    public class CommandMutes : IRocketCommand
    {
        public AllowedCaller AllowedCaller => AllowedCaller.Both;
        public string Name => "mutes";
        public string Help => "Displays a list of all or player mutes.";
        public string Syntax => "[player]";
        public List<string> Aliases => new List<string>();
        public List<string> Permissions => new List<string> { "mutes" };

        public void Execute(IRocketPlayer caller, string[] command)
        {
            var plugin = ModerationPlugin.Instance;

            if (command.Length == 0)
            {
                if (caller is UnturnedPlayer)
                {
                    UnturnedChat.Say(caller, plugin.Translate("MutesLimit"), plugin.MessageColor);
                    return;
                }

                var allMutes = plugin.DatabaseService.GetActiveMutes();
                if (allMutes.Count == 0)
                {
                    UnturnedChat.Say(caller, plugin.Translate("MutesNone"), plugin.MessageColor);
                    return;
                }

                foreach (var mute in allMutes)
                {
                    UnturnedChat.Say(caller, plugin.Translate("MutesLine", mute.Id, mute.SteamName ?? mute.SteamID.ToString(), mute.SteamID, mute.PunisherName, mute.PunisherSteamID), plugin.MessageColor);
                }
            }
            else
            {
                string targetArg = command[0];
                ulong targetSteamId = 0;
                UnturnedPlayer online = PlayerUtil.FindPlayer(targetArg);
                if (online != null)
                {
                    targetSteamId = online.CSteamID.m_SteamID;
                }
                else if (!PlayerUtil.TryParseSteamID(targetArg, out targetSteamId))
                {
                    targetSteamId = plugin.DatabaseService?.FindSteamIdByName(targetArg) ?? 0;
                }

                if (targetSteamId == 0)
                {
                    UnturnedChat.Say(caller, plugin.Translate("PlayerNotFound"), plugin.MessageColor);
                    return;
                }

                var playerMutes = plugin.DatabaseService.GetAllMutesForPlayer(targetSteamId);
                if (playerMutes == null || playerMutes.Count == 0)
                {
                    UnturnedChat.Say(caller, plugin.Translate("MutesNone"), plugin.MessageColor);
                    return;
                }

                foreach (var mute in playerMutes)
                {
                    UnturnedChat.Say(caller, plugin.Translate("MutesLine", mute.Id, mute.SteamName ?? mute.SteamID.ToString(), mute.SteamID, mute.PunisherName, mute.PunisherSteamID), plugin.MessageColor);
                }
            }
        }
    }
}
