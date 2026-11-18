using System.Collections.Generic;
using Moderation.Utils;
using Rocket.API;
using Rocket.Unturned.Chat;
using Rocket.Unturned.Player;

namespace Moderation.Commands
{
    public class CommandKick : IRocketCommand
    {
        public AllowedCaller AllowedCaller => AllowedCaller.Both;
        public string Name => "kick";
        public string Help => "Kicks specified player for reason.";
        public string Syntax => "<player> [reason]";
        public List<string> Aliases => new List<string>();
        public List<string> Permissions => new List<string> { "kick" };

        public void Execute(IRocketPlayer caller, string[] command)
        {
            var plugin = ModerationPlugin.Instance;
            if (command.Length < 1)
            {
                UnturnedChat.Say(caller, plugin.Translate("KickInvalid"), plugin.MessageColor);
                return;
            }

            UnturnedPlayer targetPlayer = PlayerUtil.FindPlayer(command[0]);
            if (targetPlayer == null)
            {
                UnturnedChat.Say(caller, plugin.Translate("PlayerNotFound"), plugin.MessageColor);
                return;
            }

            string reason = null;
            if (command.Length > 1)
            {
                reason = string.Join(" ", command, 1, command.Length - 1);
            }

            if (plugin.Configuration.Instance.Commands.ShouldRequireReason && string.IsNullOrWhiteSpace(reason))
            {
                UnturnedChat.Say(caller, plugin.Translate("RequireReason"), plugin.MessageColor);
                return;
            }

            if (string.IsNullOrWhiteSpace(reason))
            {
                reason = plugin.Translate("ReasonUnspecified");
            }

            ulong punisherSteamId = 0;
            string punisherName = "Console";
            if (caller is UnturnedPlayer unturnedCaller)
            {
                punisherSteamId = unturnedCaller.CSteamID.m_SteamID;
                punisherName = unturnedCaller.CharacterName;
            }

            plugin.ModerationService.KickPlayer(targetPlayer, reason, punisherSteamId, punisherName);
        }
    }
}
