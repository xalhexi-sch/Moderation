using System.Collections.Generic;
using Moderation.Utils;
using Rocket.API;
using Rocket.Unturned.Chat;
using Rocket.Unturned.Player;

namespace Moderation.Commands
{
    public class CommandUnmute : IRocketCommand
    {
        public AllowedCaller AllowedCaller => AllowedCaller.Both;
        public string Name => "unmute";
        public string Help => "Removes specified player latest mute.";
        public string Syntax => "<player>";
        public List<string> Aliases => new List<string>();
        public List<string> Permissions => new List<string> { "unmute" };

        public void Execute(IRocketPlayer caller, string[] command)
        {
            var plugin = ModerationPlugin.Instance;
            if (command.Length < 1)
            {
                UnturnedChat.Say(caller, plugin.Translate("UnmuteInvalid"), plugin.MessageColor);
                return;
            }

            ulong targetSteamId = 0;
            UnturnedPlayer online = PlayerUtil.FindPlayer(command[0]);
            if (online != null)
            {
                targetSteamId = online.CSteamID.m_SteamID;
            }
            else if (!PlayerUtil.TryParseSteamID(command[0], out targetSteamId))
            {
                targetSteamId = plugin.DatabaseService?.FindSteamIdByName(command[0]) ?? 0;
            }

            if (targetSteamId == 0)
            {
                UnturnedChat.Say(caller, plugin.Translate("PlayerNotFound"), plugin.MessageColor);
                return;
            }

            ulong punisherSteamId = 0;
            string punisherName = "Console";
            if (caller is UnturnedPlayer unturnedCaller)
            {
                punisherSteamId = unturnedCaller.CSteamID.m_SteamID;
                punisherName = unturnedCaller.CharacterName;
            }

            if (!plugin.ModerationService.UnmutePlayer(targetSteamId, punisherSteamId, punisherName, out string error))
            {
                UnturnedChat.Say(caller, error, plugin.MessageColor);
            }
        }
    }
}
