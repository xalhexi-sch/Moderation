using System.Collections.Generic;
using Moderation.Models;
using Moderation.Utils;
using Rocket.API;
using Rocket.Unturned.Chat;
using Rocket.Unturned.Player;
using Steamworks;

namespace Moderation.Commands
{
    public class CommandBan : IRocketCommand
    {
        public AllowedCaller AllowedCaller => AllowedCaller.Both;
        public string Name => "ban";
        public string Help => "Bans specified player for duration with reason.";
        public string Syntax => "<player> [reason] [duration]";
        public List<string> Aliases => new List<string>();
        public List<string> Permissions => new List<string> { "ban" };

        public void Execute(IRocketPlayer caller, string[] command)
        {
            var plugin = ModerationPlugin.Instance;
            if (command.Length < 1)
            {
                UnturnedChat.Say(caller, plugin.Translate("BanInvalid"), plugin.MessageColor);
                return;
            }

            string targetArg = command[0];
            UnturnedPlayer onlineTarget = PlayerUtil.FindPlayer(targetArg);
            ulong targetSteamId = 0;
            string targetName = targetArg;
            string ip = string.Empty;
            string hwid = string.Empty;

            if (onlineTarget != null)
            {
                targetSteamId = onlineTarget.CSteamID.m_SteamID;
                targetName = onlineTarget.CharacterName;
                ip = PlayerUtil.GetIP(onlineTarget);
                hwid = PlayerUtil.GetHwids(onlineTarget);
            }
            else if (PlayerUtil.TryParseSteamID(targetArg, out targetSteamId))
            {
                PlayerRecord offlineRec = plugin?.DatabaseService?.GetPlayer(targetSteamId);
                if (offlineRec != null)
                {
                    targetName = offlineRec.SteamName ?? targetSteamId.ToString();
                    ip = offlineRec.IP;
                    hwid = offlineRec.HWID;
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
                        ip = offlineRec.IP;
                        hwid = offlineRec.HWID;
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

            if (plugin?.Configuration?.Instance?.Commands?.ShouldRequireReason == true && string.IsNullOrWhiteSpace(reason))
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

            if (!plugin.ModerationService.BanPlayer(targetSteamId, targetName, ip, hwid, reason, duration, punisherSteamId, punisherName, out string error))
            {
                UnturnedChat.Say(caller, error, plugin.MessageColor);
            }
        }
    }
}
