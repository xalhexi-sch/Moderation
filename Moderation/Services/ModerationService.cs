using System;
using System.Collections.Generic;
using System.Linq;
using Moderation.Models;
using Moderation.Utils;
using Rocket.Core.Logging;
using Rocket.Unturned.Chat;
using Rocket.Unturned.Player;
using SDG.Unturned;
using Steamworks;

namespace Moderation.Services
{
    public class ModerationService
    {
        private readonly ModerationPlugin plugin;
        private readonly DatabaseService databaseService;
        private readonly DiscordWebhookService discordService;
        private readonly UCSService ucsService;

        public ModerationService(ModerationPlugin plugin, DatabaseService databaseService, DiscordWebhookService discordService, UCSService ucsService)
        {
            this.plugin = plugin;
            this.databaseService = databaseService;
            this.discordService = discordService;
            this.ucsService = ucsService;
        }

        public void Initialize()
        {
            Provider.onCheckBanStatusWithHWID += OnCheckBanStatusWithHWID;
        }

        public void Shutdown()
        {
            Provider.onCheckBanStatusWithHWID -= OnCheckBanStatusWithHWID;
        }

        private void OnCheckBanStatusWithHWID(SteamPlayerID playerID, uint remoteIP, ref bool isBanned, ref string banReason, ref uint banRemainingDuration)
        {
            if (playerID == null) return;
            ulong steamId = playerID.steamID.m_SteamID;
            string remoteIpStr = Parser.getIPFromUInt32(remoteIP);

            // 1. Direct SteamID Ban Check
            var ban = databaseService.GetActiveBan(steamId);
            if (ban != null)
            {
                if (ban.IsExpired)
                {
                    databaseService.RemoveBan(steamId, "Console");
                }
                else
                {
                    isBanned = true;
                    string timeLeft = ban.Duration == 0 ? plugin.Translate("Permanent") : DurationParser.FormatDuration(ban.RemainingSeconds);
                    banReason = plugin.Translate("BanMessage", ban.Reason ?? plugin.Translate("ReasonUnspecified"), timeLeft);
                    banRemainingDuration = ban.RemainingSeconds;
                    return;
                }
            }

            var activeBans = databaseService.GetActiveBans();

            // 2. HWID Ban Check & Evasion Detection
            if (plugin.Configuration.Instance.Commands.UseHwidBans)
            {
                IEnumerable<byte[]> clientHwids = playerID.GetHwids();
                if (clientHwids != null)
                {
                    foreach (var activeBan in activeBans)
                    {
                        if (activeBan.SteamID == steamId || activeBan.IsExpired)
                            continue;

                        if (!string.IsNullOrEmpty(activeBan.HWID))
                        {
                            string[] banHwids = activeBan.HWID.Split(';');
                            foreach (var banHex in banHwids)
                            {
                                if (string.IsNullOrWhiteSpace(banHex)) continue;
                                foreach (var clientHwid in clientHwids)
                                {
                                    if (clientHwid == null) continue;
                                    string clientHex = BitConverter.ToString(clientHwid).Replace("-", "");
                                    if (string.Equals(banHex, clientHex, StringComparison.OrdinalIgnoreCase))
                                    {
                                        // BAN EVASION DETECTED VIA HWID
                                        isBanned = true;
                                        string timeLeft = activeBan.Duration == 0 ? plugin.Translate("Permanent") : DurationParser.FormatDuration(activeBan.RemainingSeconds);
                                        banReason = plugin.Translate("BanMessage", activeBan.Reason ?? plugin.Translate("ReasonUnspecified"), timeLeft);
                                        banRemainingDuration = activeBan.RemainingSeconds;

                                        TriggerBanEvaderWebhook(playerID, "HWID", activeBan, timeLeft);
                                        return;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            // 3. IP Ban Check & Evasion Detection
            if (plugin.Configuration.Instance.Commands.UseIPBans && !string.IsNullOrEmpty(remoteIpStr) && remoteIpStr != "127.0.0.1")
            {
                foreach (var activeBan in activeBans)
                {
                    if (activeBan.SteamID == steamId || activeBan.IsExpired)
                        continue;

                    if (string.Equals(activeBan.IP, remoteIpStr, StringComparison.OrdinalIgnoreCase))
                    {
                        // BAN EVASION DETECTED VIA IP
                        isBanned = true;
                        string timeLeft = activeBan.Duration == 0 ? plugin.Translate("Permanent") : DurationParser.FormatDuration(activeBan.RemainingSeconds);
                        banReason = plugin.Translate("BanMessage", activeBan.Reason ?? plugin.Translate("ReasonUnspecified"), timeLeft);
                        banRemainingDuration = activeBan.RemainingSeconds;

                        TriggerBanEvaderWebhook(playerID, "IP", activeBan, timeLeft);
                        return;
                    }
                }
            }

            // 4. UCS Ban Check
            if (ucsService != null && ucsService.CheckUCSBan(playerID, remoteIP, out string ucsMessage))
            {
                isBanned = true;
                banReason = ucsMessage;
                banRemainingDuration = uint.MaxValue;
            }
        }

        private void TriggerBanEvaderWebhook(SteamPlayerID evaderID, string method, BanRecord originalBan, string timeLeft)
        {
            var placeholders = new Dictionary<string, string>
            {
                ["evader_name"] = evaderID.characterName ?? evaderID.playerName ?? evaderID.steamID.ToString(),
                ["evader_steamid"] = evaderID.steamID.ToString(),
                ["evader_method"] = method,
                ["name"] = originalBan.SteamName ?? originalBan.SteamID.ToString(),
                ["steamid"] = originalBan.SteamID.ToString(),
                ["reason"] = originalBan.Reason ?? plugin.Translate("ReasonUnspecified"),
                ["duration"] = originalBan.Duration == 0 ? plugin.Translate("Permanent") : DurationParser.FormatDuration(originalBan.Duration),
                ["time_left"] = timeLeft,
                ["punisher_name"] = originalBan.PunisherName ?? "Console",
                ["punisher_steamid"] = originalBan.PunisherSteamID.ToString(),
                ["server_name"] = Provider.serverName,
                ["server_icon"] = plugin.Configuration.Instance.Commands.MessageIconUrl,
                ["avatar_url"] = plugin.Configuration.Instance.Commands.MessageIconUrl
            };

            discordService.SendWebhookList(plugin.Configuration.Instance.DiscordService.BanEvaderWebhookMessages, placeholders);
        }

        public bool BanPlayer(ulong targetSteamId, string targetName, string ip, string hwid, string reason, uint duration, ulong punisherSteamId, string punisherName, out string errorMessage)
        {
            errorMessage = null;

            var existingBan = databaseService.GetActiveBan(targetSteamId);
            if (existingBan != null && !existingBan.IsExpired)
            {
                if (existingBan.Duration == 0)
                {
                    errorMessage = plugin.Translate("BanAlreadyExistsPermanent", existingBan.SteamName ?? targetName, targetSteamId, existingBan.Reason);
                    return false;
                }
                if (duration > 0 && existingBan.RemainingSeconds >= duration)
                {
                    string existingDuration = DurationParser.FormatDuration(existingBan.RemainingSeconds);
                    errorMessage = plugin.Translate("BanAlreadyExists", existingBan.SteamName ?? targetName, targetSteamId, existingBan.Reason, existingDuration);
                    return false;
                }
            }

            DateTime now = DateTime.UtcNow;
            DateTime? expiresAt = duration == 0 ? (DateTime?)null : now.AddSeconds(duration);

            var newBan = new BanRecord
            {
                SteamID = targetSteamId,
                SteamName = targetName,
                IP = ip,
                HWID = hwid,
                Reason = reason,
                Duration = duration,
                PunisherSteamID = punisherSteamId,
                PunisherName = punisherName,
                CreatedAt = now,
                ExpiresAt = expiresAt,
                IsActive = true
            };

            databaseService.AddBan(newBan);

            // Kick player if online
            UnturnedPlayer online = UnturnedPlayer.FromCSteamID(new CSteamID(targetSteamId));
            if (online != null)
            {
                string timeLeft = duration == 0 ? plugin.Translate("Permanent") : DurationParser.FormatDuration(duration);
                string kickMsg = plugin.Translate("BanMessage", reason, timeLeft);
                Provider.kick(online.CSteamID, kickMsg);
            }

            string formattedDuration = duration == 0 ? plugin.Translate("Permanent") : DurationParser.FormatDuration(duration);

            if (plugin.Configuration.Instance.Commands.ShouldBroadcastBan)
            {
                UnturnedChat.Say(plugin.Translate("BanBroadcast", targetName, punisherName, reason, formattedDuration), plugin.MessageColor);
            }

            var placeholders = new Dictionary<string, string>
            {
                ["name"] = targetName,
                ["steamid"] = targetSteamId.ToString(),
                ["hwid"] = hwid ?? string.Empty,
                ["ip"] = ip ?? string.Empty,
                ["reason"] = reason,
                ["duration"] = formattedDuration,
                ["punisher_name"] = punisherName,
                ["punisher_steamid"] = punisherSteamId.ToString(),
                ["server_name"] = Provider.serverName,
                ["server_icon"] = plugin.Configuration.Instance.Commands.MessageIconUrl,
                ["avatar_url"] = plugin.Configuration.Instance.Commands.MessageIconUrl
            };

            discordService.SendWebhookList(plugin.Configuration.Instance.DiscordService.BanWebhookMessages, placeholders);
            return true;
        }

        public bool UnbanPlayer(ulong targetSteamId, ulong punisherSteamId, string punisherName, out string errorMessage)
        {
            errorMessage = null;
            var existingBan = databaseService.GetActiveBan(targetSteamId);
            if (existingBan == null)
            {
                errorMessage = plugin.Translate("UnbanFail", targetSteamId);
                return false;
            }

            databaseService.RemoveBan(targetSteamId, punisherName);
            SteamBlacklist.unban(new CSteamID(targetSteamId));

            string targetName = existingBan.SteamName ?? targetSteamId.ToString();

            if (plugin.Configuration.Instance.Commands.ShouldBroadcastUnban)
            {
                UnturnedChat.Say(plugin.Translate("UnbanBroadcast", targetName), plugin.MessageColor);
            }

            var placeholders = new Dictionary<string, string>
            {
                ["name"] = targetName,
                ["steamid"] = targetSteamId.ToString(),
                ["punisher_name"] = punisherName,
                ["punisher_steamid"] = punisherSteamId.ToString(),
                ["server_name"] = Provider.serverName,
                ["server_icon"] = plugin.Configuration.Instance.Commands.MessageIconUrl,
                ["avatar_url"] = plugin.Configuration.Instance.Commands.MessageIconUrl
            };

            discordService.SendWebhookList(plugin.Configuration.Instance.DiscordService.UnbanWebhookMessages, placeholders);
            return true;
        }

        public void MutePlayer(ulong targetSteamId, string targetName, string reason, uint duration, ulong punisherSteamId, string punisherName)
        {
            DateTime now = DateTime.UtcNow;
            DateTime? expiresAt = duration == 0 ? (DateTime?)null : now.AddSeconds(duration);

            var newMute = new MuteRecord
            {
                SteamID = targetSteamId,
                SteamName = targetName,
                Reason = reason,
                Duration = duration,
                PunisherSteamID = punisherSteamId,
                PunisherName = punisherName,
                CreatedAt = now,
                ExpiresAt = expiresAt,
                IsActive = true
            };

            databaseService.AddMute(newMute);

            string formattedDuration = duration == 0 ? plugin.Translate("Permanent") : DurationParser.FormatDuration(duration);

            UnturnedPlayer online = UnturnedPlayer.FromCSteamID(new CSteamID(targetSteamId));
            if (online != null)
            {
                if (plugin.Configuration.Instance.MuteService.EnableVoiceChatMute)
                {
                    online.Player.voice.ServerSetPermissions(online.Player.voice.GetAllowTalkingWhileDead(), false);
                }
                UnturnedChat.Say(online, plugin.Translate("MuteMessage", reason, formattedDuration), plugin.MessageColor);
            }

            UnturnedChat.Say(plugin.Translate("MuteAnnouncement", targetName, punisherName, reason, formattedDuration), plugin.MessageColor);

            var placeholders = new Dictionary<string, string>
            {
                ["name"] = targetName,
                ["steamid"] = targetSteamId.ToString(),
                ["reason"] = reason,
                ["duration"] = formattedDuration,
                ["punisher_name"] = punisherName,
                ["punisher_steamid"] = punisherSteamId.ToString(),
                ["server_name"] = Provider.serverName,
                ["server_icon"] = plugin.Configuration.Instance.Commands.MessageIconUrl,
                ["avatar_url"] = plugin.Configuration.Instance.Commands.MessageIconUrl
            };

            discordService.SendWebhookList(plugin.Configuration.Instance.DiscordService.MuteWebhookMessages, placeholders);
        }

        public bool UnmutePlayer(ulong targetSteamId, ulong punisherSteamId, string punisherName, out string errorMessage)
        {
            errorMessage = null;
            var existingMute = databaseService.GetActiveMute(targetSteamId);
            if (existingMute == null)
            {
                errorMessage = plugin.Translate("UnmuteNotMuted", targetSteamId);
                return false;
            }

            databaseService.RemoveMute(targetSteamId, punisherName);

            UnturnedPlayer online = UnturnedPlayer.FromCSteamID(new CSteamID(targetSteamId));
            if (online != null)
            {
                online.Player.voice.ServerSetPermissions(online.Player.voice.GetAllowTalkingWhileDead(), true);
            }

            string targetName = existingMute.SteamName ?? targetSteamId.ToString();
            UnturnedChat.Say(plugin.Translate("UnmuteAnnouncement", targetName), plugin.MessageColor);

            var placeholders = new Dictionary<string, string>
            {
                ["name"] = targetName,
                ["steamid"] = targetSteamId.ToString(),
                ["punisher_name"] = punisherName,
                ["punisher_steamid"] = punisherSteamId.ToString(),
                ["server_name"] = Provider.serverName,
                ["server_icon"] = plugin.Configuration.Instance.Commands.MessageIconUrl,
                ["avatar_url"] = plugin.Configuration.Instance.Commands.MessageIconUrl
            };

            discordService.SendWebhookList(plugin.Configuration.Instance.DiscordService.UnmuteWebhookMessages, placeholders);
            return true;
        }

        public void KickPlayer(UnturnedPlayer targetPlayer, string reason, ulong punisherSteamId, string punisherName)
        {
            databaseService.AddPunishment(new PunishmentRecord
            {
                Type = "Kick",
                SteamID = targetPlayer.CSteamID.m_SteamID,
                Reason = reason,
                PunisherSteamID = punisherSteamId,
                PunisherName = punisherName,
                CreatedAt = DateTime.UtcNow
            });

            UnturnedChat.Say(plugin.Translate("KickAnnouncement", targetPlayer.CharacterName, punisherName, reason), plugin.MessageColor);

            var placeholders = new Dictionary<string, string>
            {
                ["name"] = targetPlayer.CharacterName,
                ["steamid"] = targetPlayer.CSteamID.m_SteamID.ToString(),
                ["reason"] = reason,
                ["punisher_name"] = punisherName,
                ["punisher_steamid"] = punisherSteamId.ToString(),
                ["server_name"] = Provider.serverName,
                ["server_icon"] = plugin.Configuration.Instance.Commands.MessageIconUrl,
                ["avatar_url"] = plugin.Configuration.Instance.Commands.MessageIconUrl
            };

            discordService.SendWebhookList(plugin.Configuration.Instance.DiscordService.KickWebhookMessages, placeholders);
            Provider.kick(targetPlayer.CSteamID, reason);
        }

        public void WarnPlayer(UnturnedPlayer targetPlayer, string reason, ulong punisherSteamId, string punisherName)
        {
            databaseService.AddPunishment(new PunishmentRecord
            {
                Type = "Warn",
                SteamID = targetPlayer.CSteamID.m_SteamID,
                Reason = reason,
                PunisherSteamID = punisherSteamId,
                PunisherName = punisherName,
                CreatedAt = DateTime.UtcNow
            });

            UnturnedChat.Say(plugin.Translate("WarnAnnouncement", targetPlayer.CharacterName, punisherName, reason), plugin.MessageColor);

            var placeholders = new Dictionary<string, string>
            {
                ["name"] = targetPlayer.CharacterName,
                ["steamid"] = targetPlayer.CSteamID.m_SteamID.ToString(),
                ["reason"] = reason,
                ["punisher_name"] = punisherName,
                ["punisher_steamid"] = punisherSteamId.ToString(),
                ["server_name"] = Provider.serverName,
                ["server_icon"] = plugin.Configuration.Instance.Commands.MessageIconUrl,
                ["avatar_url"] = plugin.Configuration.Instance.Commands.MessageIconUrl
            };

            discordService.SendWebhookList(plugin.Configuration.Instance.DiscordService.WarnWebhookMessages, placeholders);
        }
    }
}
