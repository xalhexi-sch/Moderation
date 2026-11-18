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

            if (!PlayerUtil.TryParseSteamID(command[0], out ulong targetSteamId))
            {
                UnturnedChat.Say(caller, plugin.Translate("UnmuteInvalid"), plugin.MessageColor);
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
