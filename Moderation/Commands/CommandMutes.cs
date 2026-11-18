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
            var allMutes = plugin.DatabaseService.GetActiveMutes();

            if (command.Length == 0)
            {
                if (caller is UnturnedPlayer)
                {
                    UnturnedChat.Say(caller, plugin.Translate("MutesLimit"), plugin.MessageColor);
                    return;
                }

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
                if (!PlayerUtil.TryParseSteamID(command[0], out ulong targetSteamId))
                {
                    UnturnedChat.Say(caller, plugin.Translate("PlayerNotFound"), plugin.MessageColor);
                    return;
                }

                var playerMutes = allMutes.FindAll(m => m.SteamID == targetSteamId);
                if (playerMutes.Count == 0)
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
