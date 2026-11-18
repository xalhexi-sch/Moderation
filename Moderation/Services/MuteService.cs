using System;
using System.Collections.Generic;
using System.Threading;
using Moderation.Models;
using Moderation.Utils;
using Rocket.API;
using Rocket.Core;
using Rocket.Core.Utils;
using Rocket.Unturned.Chat;
using Rocket.Unturned.Player;
using SDG.Unturned;
using Steamworks;
using UnityEngine;
using Logger = Rocket.Core.Logging.Logger;

namespace Moderation.Services
{
    public class MuteService
    {
        private readonly ModerationPlugin plugin;
        private readonly MuteServiceConfiguration config;
        private readonly DatabaseService databaseService;
        private readonly DiscordWebhookService discordService;
        private Timer unmuteTimer;
        private readonly Dictionary<ulong, DateTime> voiceThrottle = new Dictionary<ulong, DateTime>();
        private readonly object throttleLock = new object();

        public MuteService(ModerationPlugin plugin, MuteServiceConfiguration config, DatabaseService databaseService, DiscordWebhookService discordService)
        {
            this.plugin = plugin;
            this.config = config ?? new MuteServiceConfiguration();
            this.databaseService = databaseService;
            this.discordService = discordService;
        }

        public void Initialize()
        {
            ChatManager.onChatted += OnChatted;
            PlayerVoice.onRelayVoice += OnRelayVoice;
            R.Commands.OnExecuteCommand += OnExecuteCommand;

            int intervalMs = Math.Max(5, config.UnmuteRefreshTimeSeconds) * 1000;
            unmuteTimer = new Timer(CheckExpiredMutes, null, intervalMs, intervalMs);
        }

        public void Shutdown()
        {
            ChatManager.onChatted -= OnChatted;
            PlayerVoice.onRelayVoice -= OnRelayVoice;
            R.Commands.OnExecuteCommand -= OnExecuteCommand;

            unmuteTimer?.Dispose();
            unmuteTimer = null;
        }

        public void OnPlayerConnected(UnturnedPlayer player)
        {
            if (player == null) return;
            try
            {
                ulong steamId = player.CSteamID.m_SteamID;
                if (steamId == 0) return;

                var mute = databaseService?.GetActiveMute(steamId);
                if (mute != null && mute.IsCurrentlyActive && config.EnableVoiceChatMute)
                {
                    if (player.Player?.voice != null)
                    {
                        player.Player.voice.ServerSetPermissions(player.Player.voice.GetAllowTalkingWhileDead(), false);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogWarning($"[Moderation] Exception in MuteService.OnPlayerConnected: {ex.Message}");
            }
        }

        private void OnChatted(SteamPlayer player, EChatMode mode, ref Color chatted, ref bool isRich, string text, ref bool isVisible)
        {
            if (player == null || ReferenceEquals(player.playerID, null)) return;
            try
            {
                ulong steamId = player.playerID.steamID.m_SteamID;
                var mute = databaseService?.GetActiveMute(steamId);
                if (mute != null && mute.IsCurrentlyActive)
                {
                    isVisible = false;
                    string reason = string.IsNullOrEmpty(mute.Reason) ? (plugin?.Translate("ReasonUnspecified") ?? "Unspecified") : mute.Reason;
                    string duration = mute.Duration == 0 ? (plugin?.Translate("Permanent") ?? "permanent") : DurationParser.FormatDuration(mute.RemainingSeconds);
                    UnturnedChat.Say(player.playerID.steamID, plugin?.Translate("MuteMessage", reason, duration) ?? $"You are muted for {reason} ({duration})", plugin?.MessageColor ?? Color.grey);
                }
            }
            catch (Exception ex)
            {
                Logger.LogWarning($"[Moderation] Exception in OnChatted: {ex.Message}");
            }
        }

        private void OnRelayVoice(PlayerVoice speaker, bool wantsToUseWalkieTalkie, ref bool shouldAllow, ref bool shouldBroadcastOverRadio, ref PlayerVoice.RelayVoiceCullingHandler cullingHandler)
        {
            if (speaker?.player?.channel?.owner == null || ReferenceEquals(speaker.player.channel.owner.playerID, null)) return;
            try
            {
                ulong steamId = speaker.player.channel.owner.playerID.steamID.m_SteamID;
                var mute = databaseService?.GetActiveMute(steamId);
                if (mute != null && mute.IsCurrentlyActive)
                {
                    shouldAllow = false;
                    if (config.EnableVoiceChatMute)
                    {
                        speaker.ServerSetPermissions(speaker.GetAllowTalkingWhileDead(), false);
                        if (ShouldThrottleVoice(steamId))
                        {
                            string reason = string.IsNullOrEmpty(mute.Reason) ? (plugin?.Translate("ReasonUnspecified") ?? "Unspecified") : mute.Reason;
                            string duration = mute.Duration == 0 ? (plugin?.Translate("Permanent") ?? "permanent") : DurationParser.FormatDuration(mute.RemainingSeconds);
                            UnturnedChat.Say(speaker.player.channel.owner.playerID.steamID, plugin?.Translate("MuteVoice", reason, duration) ?? $"Voice chat blocked: {reason} ({duration})", plugin?.MessageColor ?? Color.grey);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogWarning($"[Moderation] Exception in OnRelayVoice: {ex.Message}");
            }
        }

        private bool ShouldThrottleVoice(ulong steamId)
        {
            lock (throttleLock)
            {
                int throttleSec = Math.Max(1, config.VoiceChatThrottleMessagesSeconds);
                if (voiceThrottle.TryGetValue(steamId, out DateTime lastTime))
                {
                    if ((DateTime.UtcNow - lastTime).TotalSeconds < throttleSec)
                        return false;
                }
                voiceThrottle[steamId] = DateTime.UtcNow;
                return true;
            }
        }

        private void OnExecuteCommand(IRocketPlayer player, IRocketCommand command, ref bool cancel)
        {
            if (!config.EnableBlockCommands || config.BlockCommands == null || player == null || player is ConsolePlayer || command == null)
                return;

            try
            {
                if (ulong.TryParse(player.Id, out ulong steamId))
                {
                    var mute = databaseService?.GetActiveMute(steamId);
                    if (mute != null && mute.IsCurrentlyActive)
                    {
                        if (config.BlockCommands.Exists(c => string.Equals(c, command.Name, StringComparison.OrdinalIgnoreCase)))
                        {
                            cancel = true;
                            string reason = string.IsNullOrEmpty(mute.Reason) ? (plugin?.Translate("ReasonUnspecified") ?? "Unspecified") : mute.Reason;
                            string duration = mute.Duration == 0 ? (plugin?.Translate("Permanent") ?? "permanent") : DurationParser.FormatDuration(mute.RemainingSeconds);
                            UnturnedChat.Say(player, plugin?.Translate("MuteBlockCommand", reason, duration) ?? $"Command blocked: {reason} ({duration})", plugin?.MessageColor ?? Color.grey);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogWarning($"[Moderation] Exception in OnExecuteCommand: {ex.Message}");
            }
        }

        private void CheckExpiredMutes(object state)
        {
            try
            {
                var mutes = databaseService?.GetActiveMutes();
                if (mutes == null) return;

                foreach (var mute in mutes)
                {
                    if (mute != null && mute.IsActive && mute.Duration > 0 && mute.ExpiresAt.HasValue && DateTime.UtcNow >= mute.ExpiresAt.Value)
                    {
                        // Mark inactive immediately in-memory so subsequent ticks don't re-trigger
                        mute.IsActive = false;
                        ulong muteSteamId = mute.SteamID;
                        string muteSteamName = mute.SteamName ?? muteSteamId.ToString();

                        databaseService?.RemoveMute(muteSteamId, "Console");

                        // Queue voice chat permission restore and player notification onto main thread
                        TaskDispatcher.QueueOnMainThread(() =>
                        {
                            try
                            {
                                UnturnedPlayer online = UnturnedPlayer.FromCSteamID(new CSteamID(muteSteamId));
                                if (online != null)
                                {
                                    if (online.Player?.voice != null)
                                    {
                                        online.Player.voice.ServerSetPermissions(online.Player.voice.GetAllowTalkingWhileDead(), true);
                                    }
                                    UnturnedChat.Say(online, plugin?.Translate("UnmuteExpired", muteSteamName) ?? "Your mute has expired.", plugin?.MessageColor ?? Color.grey);
                                }
                            }
                            catch (Exception ex)
                            {
                                Logger.LogWarning($"[Moderation] Error restoring player voice on mute expiration: {ex.Message}");
                            }
                        });

                        if (config.EnableUnmuteWebhooks && discordService != null)
                        {
                            var placeholders = new Dictionary<string, string>
                            {
                                ["name"] = muteSteamName,
                                ["steamid"] = muteSteamId.ToString(),
                                ["reason"] = mute.Reason ?? "Unspecified",
                                ["punisher_name"] = mute.PunisherName ?? "Console",
                                ["punisher_steamid"] = mute.PunisherSteamID.ToString(),
                                ["server_name"] = Provider.serverName ?? "Unturned Server",
                                ["server_icon"] = plugin?.Configuration?.Instance?.Commands?.MessageIconUrl ?? string.Empty,
                                ["avatar_url"] = plugin?.Configuration?.Instance?.Commands?.MessageIconUrl ?? string.Empty
                            };
                            var webhooks = plugin?.Configuration?.Instance?.DiscordService?.MuteExpiredWebhookMessages;
                            if (webhooks != null)
                            {
                                discordService.SendWebhookList(webhooks, placeholders);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogWarning($"[Moderation] Error in CheckExpiredMutes: {ex.Message}");
            }
        }
    }
}
