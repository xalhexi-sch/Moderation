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
            try
            {
                if (playerID == null) return;
                ulong steamId = playerID.steamID.m_SteamID;
                string remoteIpStr = Parser.getIPFromUInt32(remoteIP);

                // 1. Direct SteamID Ban Check
                if (databaseService != null)
                {
                    var ban = databaseService.GetActiveBan(steamId);
                    if (ban != null)
                    {
                        if (!ban.IsCurrentlyActive)
                        {
                            databaseService.RemoveBan(steamId, "Console");
                        }
                        else
                        {
                            isBanned = true;
                            string timeLeft = ban.Duration == 0 ? (plugin?.Translate("Permanent") ?? "permanent") : DurationParser.FormatDuration(ban.RemainingSeconds);
                            string reason = ban.Reason ?? (plugin?.Translate("ReasonUnspecified") ?? "Unspecified");
                            banReason = plugin?.Translate("BanMessage", reason, timeLeft) ?? $"[BAN] Reason: {reason} Time Left: {timeLeft}";
                            banRemainingDuration = ban.RemainingSeconds;
                            return;
                        }
                    }
                }

                var commandsConfig = plugin?.Configuration?.Instance?.Commands ?? new CommandsConfiguration();
                var activeBans = databaseService?.GetActiveBans();

                // 2. HWID Ban Check & Evasion Detection
                if (commandsConfig.UseHwidBans && activeBans != null && activeBans.Count > 0)
                {
                    IEnumerable<byte[]> clientHwids = null;
                    try
                    {
                        clientHwids = playerID.GetHwids();
                    }
                    catch { }

                    if (clientHwids != null)
                    {
                        foreach (var activeBan in activeBans)
                        {
                            if (activeBan == null || activeBan.SteamID == steamId || !activeBan.IsCurrentlyActive)
                                continue;

                            if (!string.IsNullOrEmpty(activeBan.HWID))
                            {
                                string[] banHwids = activeBan.HWID.Split(';');
                                foreach (var banHex in banHwids)
                                {
                                    if (string.IsNullOrWhiteSpace(banHex)) continue;
                                    foreach (var clientHwid in clientHwids)
                                    {
                                        if (clientHwid == null || clientHwid.Length == 0) continue;
                                        string clientHex = BitConverter.ToString(clientHwid).Replace("-", "");
                                        if (string.Equals(banHex.Trim(), clientHex, StringComparison.OrdinalIgnoreCase))
                                        {
                                            // BAN EVASION DETECTED VIA HWID
                                            isBanned = true;
                                            string timeLeft = activeBan.Duration == 0 ? (plugin?.Translate("Permanent") ?? "permanent") : DurationParser.FormatDuration(activeBan.RemainingSeconds);
                                            string reason = activeBan.Reason ?? (plugin?.Translate("ReasonUnspecified") ?? "Unspecified");
                                            banReason = plugin?.Translate("BanMessage", reason, timeLeft) ?? $"[BAN] Reason: {reason} Time Left: {timeLeft}";
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
                if (commandsConfig.UseIPBans && activeBans != null && activeBans.Count > 0 && !string.IsNullOrEmpty(remoteIpStr) && remoteIpStr != "127.0.0.1")
                {
                    foreach (var activeBan in activeBans)
                    {
                        if (activeBan == null || activeBan.SteamID == steamId || !activeBan.IsCurrentlyActive)
                            continue;

                        if (!string.IsNullOrEmpty(activeBan.IP) && string.Equals(activeBan.IP, remoteIpStr, StringComparison.OrdinalIgnoreCase))
                        {
                            // BAN EVASION DETECTED VIA IP
                            isBanned = true;
                            string timeLeft = activeBan.Duration == 0 ? (plugin?.Translate("Permanent") ?? "permanent") : DurationParser.FormatDuration(activeBan.RemainingSeconds);
                            string reason = activeBan.Reason ?? (plugin?.Translate("ReasonUnspecified") ?? "Unspecified");
                            banReason = plugin?.Translate("BanMessage", reason, timeLeft) ?? $"[BAN] Reason: {reason} Time Left: {timeLeft}";
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
            catch (Exception ex)
            {
                Logger.LogWarning($"[Moderation] Handled error in OnCheckBanStatusWithHWID: {ex.Message}");
            }
        }

        private void TriggerBanEvaderWebhook(SteamPlayerID evaderID, string method, BanRecord originalBan, string timeLeft)
        {
            if (discordService == null || originalBan == null || evaderID == null) return;
            try
            {
                var placeholders = new Dictionary<string, string>
                {
                    ["evader_name"] = evaderID.characterName ?? evaderID.playerName ?? evaderID.steamID.ToString(),
                    ["evader_steamid"] = evaderID.steamID.ToString(),
                    ["evader_method"] = method ?? "Unknown",
                    ["name"] = originalBan.SteamName ?? originalBan.SteamID.ToString(),
                    ["steamid"] = originalBan.SteamID.ToString(),
                    ["reason"] = originalBan.Reason ?? (plugin?.Translate("ReasonUnspecified") ?? "Unspecified"),
                    ["duration"] = originalBan.Duration == 0 ? (plugin?.Translate("Permanent") ?? "permanent") : DurationParser.FormatDuration(originalBan.Duration),
                    ["time_left"] = timeLeft ?? string.Empty,
                    ["punisher_name"] = originalBan.PunisherName ?? "Console",
                    ["punisher_steamid"] = originalBan.PunisherSteamID.ToString(),
                    ["server_name"] = Provider.serverName ?? "Unturned Server",
                    ["server_icon"] = plugin?.Configuration?.Instance?.Commands?.MessageIconUrl ?? string.Empty,
                    ["avatar_url"] = plugin?.Configuration?.Instance?.Commands?.MessageIconUrl ?? string.Empty
                };

                var webhooks = plugin?.Configuration?.Instance?.DiscordService?.BanEvaderWebhookMessages;
                if (webhooks != null)
                {
                    discordService.SendWebhookList(webhooks, placeholders);
                }
            }
            catch (Exception ex)
            {
                Logger.LogWarning($"[Moderation] Error sending ban evader webhook: {ex.Message}");
            }
        }

        public bool BanPlayer(ulong targetSteamId, string targetName, string ip, string hwid, string reason, uint duration, ulong punisherSteamId, string punisherName, out string errorMessage)
        {
            errorMessage = null;

            if (databaseService != null)
            {
                var existingBan = databaseService.GetActiveBan(targetSteamId);
                if (existingBan != null && existingBan.IsCurrentlyActive)
                {
                    if (existingBan.Duration == 0)
                    {
                        errorMessage = plugin?.Translate("BanAlreadyExistsPermanent", existingBan.SteamName ?? targetName, targetSteamId, existingBan.Reason) ?? "Player is already permanently banned.";
                        return false;
                    }
                    if (duration > 0 && existingBan.RemainingSeconds >= duration)
                    {
                        string existingDuration = DurationParser.FormatDuration(existingBan.RemainingSeconds);
                        errorMessage = plugin?.Translate("BanAlreadyExists", existingBan.SteamName ?? targetName, targetSteamId, existingBan.Reason, existingDuration) ?? "Player is already banned for longer.";
                        return false;
                    }
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

            databaseService?.AddBan(newBan);

            // Synchronize with native Unturned blacklist
            try
            {
                uint ipUint = string.IsNullOrEmpty(ip) ? 0 : Parser.getUInt32FromIP(ip);
                List<byte[]> hwidBytes = null;
                if (!string.IsNullOrEmpty(hwid))
                {
                    hwidBytes = new List<byte[]>();
                    string[] parts = hwid.Split(';');
                    foreach (var part in parts)
                    {
                        if (string.IsNullOrWhiteSpace(part)) continue;
                        try
                        {
                            string clean = part.Trim();
                            byte[] bytes = new byte[clean.Length / 2];
                            for (int i = 0; i < bytes.Length; i++)
                            {
                                bytes[i] = Convert.ToByte(clean.Substring(i * 2, 2), 16);
                            }
                            hwidBytes.Add(bytes);
                        }
                        catch { }
                    }
                }
                SteamBlacklist.ban(new CSteamID(targetSteamId), ipUint, hwidBytes, new CSteamID(punisherSteamId), reason, duration);
            }
            catch (Exception ex)
            {
                Logger.LogWarning($"[Moderation] Failed to sync native blacklist ban: {ex.Message}");
            }

            // Kick player if online
            UnturnedPlayer online = UnturnedPlayer.FromCSteamID(new CSteamID(targetSteamId));
            if (online != null)
            {
                string timeLeft = duration == 0 ? (plugin?.Translate("Permanent") ?? "permanent") : DurationParser.FormatDuration(duration);
                string kickMsg = plugin?.Translate("BanMessage", reason, timeLeft) ?? $"[BAN] Reason: {reason} Time Left: {timeLeft}";
                Provider.kick(online.CSteamID, kickMsg);
            }

            string formattedDuration = duration == 0 ? (plugin?.Translate("Permanent") ?? "permanent") : DurationParser.FormatDuration(duration);

            if (plugin?.Configuration?.Instance?.Commands?.ShouldBroadcastBan == true)
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
                ["server_name"] = Provider.serverName ?? "Unturned Server",
                ["server_icon"] = plugin?.Configuration?.Instance?.Commands?.MessageIconUrl ?? string.Empty,
                ["avatar_url"] = plugin?.Configuration?.Instance?.Commands?.MessageIconUrl ?? string.Empty
            };

            var webhooks = plugin?.Configuration?.Instance?.DiscordService?.BanWebhookMessages;
            if (webhooks != null && discordService != null)
            {
                discordService.SendWebhookList(webhooks, placeholders);
            }
            return true;
        }

        public bool UnbanPlayer(ulong targetSteamId, ulong punisherSteamId, string punisherName, out string errorMessage)
        {
            errorMessage = null;
            var existingBan = databaseService?.GetActiveBan(targetSteamId);
            if (existingBan == null)
            {
                errorMessage = plugin?.Translate("UnbanFail", targetSteamId) ?? $"{targetSteamId} does not have an active ban.";
                return false;
            }

            databaseService?.RemoveBan(targetSteamId, punisherName);

            try
            {
                SteamBlacklist.unban(new CSteamID(targetSteamId));
            }
            catch (Exception ex)
            {
                Logger.LogWarning($"[Moderation] Failed to sync native blacklist unban: {ex.Message}");
            }

            string targetName = existingBan.SteamName ?? targetSteamId.ToString();

            if (plugin?.Configuration?.Instance?.Commands?.ShouldBroadcastUnban == true)
            {
                UnturnedChat.Say(plugin.Translate("UnbanBroadcast", targetName), plugin.MessageColor);
            }

            var placeholders = new Dictionary<string, string>
            {
                ["name"] = targetName,
                ["steamid"] = targetSteamId.ToString(),
                ["punisher_name"] = punisherName,
                ["punisher_steamid"] = punisherSteamId.ToString(),
                ["server_name"] = Provider.serverName ?? "Unturned Server",
                ["server_icon"] = plugin?.Configuration?.Instance?.Commands?.MessageIconUrl ?? string.Empty,
                ["avatar_url"] = plugin?.Configuration?.Instance?.Commands?.MessageIconUrl ?? string.Empty
            };

            var webhooks = plugin?.Configuration?.Instance?.DiscordService?.UnbanWebhookMessages;
            if (webhooks != null && discordService != null)
            {
                discordService.SendWebhookList(webhooks, placeholders);
            }
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

            databaseService?.AddMute(newMute);

            string formattedDuration = duration == 0 ? (plugin?.Translate("Permanent") ?? "permanent") : DurationParser.FormatDuration(duration);

            UnturnedPlayer online = UnturnedPlayer.FromCSteamID(new CSteamID(targetSteamId));
            if (online != null)
            {
                if (plugin?.Configuration?.Instance?.MuteService?.EnableVoiceChatMute == true && online.Player?.voice != null)
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
                ["server_name"] = Provider.serverName ?? "Unturned Server",
                ["server_icon"] = plugin?.Configuration?.Instance?.Commands?.MessageIconUrl ?? string.Empty,
                ["avatar_url"] = plugin?.Configuration?.Instance?.Commands?.MessageIconUrl ?? string.Empty
            };

            var webhooks = plugin?.Configuration?.Instance?.DiscordService?.MuteWebhookMessages;
            if (webhooks != null && discordService != null)
            {
                discordService.SendWebhookList(webhooks, placeholders);
            }
        }

        public bool UnmutePlayer(ulong targetSteamId, ulong punisherSteamId, string punisherName, out string errorMessage)
        {
            errorMessage = null;
            var existingMute = databaseService?.GetActiveMute(targetSteamId);
            if (existingMute == null)
            {
                errorMessage = plugin?.Translate("UnmuteNotMuted", targetSteamId) ?? $"{targetSteamId} is not muted.";
                return false;
            }

            databaseService?.RemoveMute(targetSteamId, punisherName);

            UnturnedPlayer online = UnturnedPlayer.FromCSteamID(new CSteamID(targetSteamId));
            if (online?.Player?.voice != null)
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
                ["server_name"] = Provider.serverName ?? "Unturned Server",
                ["server_icon"] = plugin?.Configuration?.Instance?.Commands?.MessageIconUrl ?? string.Empty,
                ["avatar_url"] = plugin?.Configuration?.Instance?.Commands?.MessageIconUrl ?? string.Empty
            };

            var webhooks = plugin?.Configuration?.Instance?.DiscordService?.UnmuteWebhookMessages;
            if (webhooks != null && discordService != null)
            {
                discordService.SendWebhookList(webhooks, placeholders);
            }
            return true;
        }

        public void KickPlayer(UnturnedPlayer targetPlayer, string reason, ulong punisherSteamId, string punisherName)
        {
            if (targetPlayer == null) return;

            databaseService?.AddPunishment(new PunishmentRecord
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
                ["server_name"] = Provider.serverName ?? "Unturned Server",
                ["server_icon"] = plugin?.Configuration?.Instance?.Commands?.MessageIconUrl ?? string.Empty,
                ["avatar_url"] = plugin?.Configuration?.Instance?.Commands?.MessageIconUrl ?? string.Empty
            };

            var webhooks = plugin?.Configuration?.Instance?.DiscordService?.KickWebhookMessages;
            if (webhooks != null && discordService != null)
            {
                discordService.SendWebhookList(webhooks, placeholders);
            }
            Provider.kick(targetPlayer.CSteamID, reason);
        }

        public void WarnPlayer(UnturnedPlayer targetPlayer, string reason, ulong punisherSteamId, string punisherName)
        {
            if (targetPlayer == null) return;

            databaseService?.AddPunishment(new PunishmentRecord
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
                ["server_name"] = Provider.serverName ?? "Unturned Server",
                ["server_icon"] = plugin?.Configuration?.Instance?.Commands?.MessageIconUrl ?? string.Empty,
                ["avatar_url"] = plugin?.Configuration?.Instance?.Commands?.MessageIconUrl ?? string.Empty
            };

            var webhooks = plugin?.Configuration?.Instance?.DiscordService?.WarnWebhookMessages;
            if (webhooks != null && discordService != null)
            {
                discordService.SendWebhookList(webhooks, placeholders);
            }
        }
    }
}
