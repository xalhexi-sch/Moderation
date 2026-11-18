using System.Collections.Generic;
using Moderation.Models;
using Moderation.Utils;
using Rocket.API;
using Rocket.Unturned.Chat;
using Rocket.Unturned.Player;

namespace Moderation.Commands
{
    public class CommandMute : IRocketCommand
    {
        public AllowedCaller AllowedCaller => AllowedCaller.Both;
        public string Name => "mute";
        public string Help => "Mutes specified player for duration with reason.";
        public string Syntax => "<player> [reason] [duration]";
        public List<string> Aliases => new List<string>();
        public List<string> Permissions => new List<string> { "mute" };

        public void Execute(IRocketPlayer caller, string[] command)
        {
            var plugin = ModerationPlugin.Instance;
            if (command.Length < 1)
            {
                UnturnedChat.Say(caller, plugin.Translate("MuteInvalid"), plugin.MessageColor);
                return;
            }

            string targetArg = command[0];
            UnturnedPlayer onlineTarget = PlayerUtil.FindPlayer(targetArg);
            ulong targetSteamId = 0;
            string targetName = targetArg;

            if (onlineTarget != null)
            {
                targetSteamId = onlineTarget.CSteamID.m_SteamID;
                targetName = onlineTarget.CharacterName;
            }
            else if (PlayerUtil.TryParseSteamID(targetArg, out targetSteamId))
            {
                PlayerRecord offlineRec = plugin.DatabaseService.GetPlayer(targetSteamId);
                if (offlineRec != null)
                {
                    targetName = offlineRec.SteamName ?? targetSteamId.ToString();
                }
            }
            else
            {
                targetSteamId = plugin?.DatabaseService?.FindSteamIdByName(targetArg) ?? 0;
                if (targetSteamId != 0)
                {
                    PlayerRecord offlineRec = plugin?.DatabaseService?.GetPlayer(targetSteamId);
                    if (offlineRec != null)
                    {
                        targetName = offlineRec.SteamName ?? targetSteamId.ToString();
                    }
                }
                else
                {
                    UnturnedChat.Say(caller, plugin.Translate("PlayerNotFound"), plugin.MessageColor);
                    return;
                }
            }

            uint duration = 0;
            string reason = null;

            if (command.Length > 1)
            {
                string lastArg = command[command.Length - 1];
                if (DurationParser.TryParse(lastArg, out uint parsedSec, out _))
                {
                    duration = parsedSec;
                    if (command.Length > 2)
                    {
                        reason = string.Join(" ", command, 1, command.Length - 2);
                    }
                }
                else
                {
                    reason = string.Join(" ", command, 1, command.Length - 1);
                }
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

            plugin.ModerationService.MutePlayer(targetSteamId, targetName, reason, duration, punisherSteamId, punisherName);
        }
    }
}
