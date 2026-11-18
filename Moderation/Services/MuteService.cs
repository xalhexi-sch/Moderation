using System;
using System.Collections.Generic;
using System.Threading;
using Moderation.Models;
using Moderation.Utils;
using Rocket.API;
using Rocket.Core;
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
            this.config = config;
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
            ulong steamId = player.CSteamID.m_SteamID;
            var mute = databaseService.GetActiveMute(steamId);
            if (mute != null && !mute.IsExpired && config.EnableVoiceChatMute)
            {
                player.Player.voice.ServerSetPermissions(player.Player.voice.GetAllowTalkingWhileDead(), false);
            }
        }

        private void OnChatted(SteamPlayer player, EChatMode mode, ref Color chatted, ref bool isRich, string text, ref bool isVisible)
        {
            if (player == null) return;
            ulong steamId = player.playerID.steamID.m_SteamID;
            var mute = databaseService.GetActiveMute(steamId);
            if (mute != null && !mute.IsExpired)
            {
                isVisible = false;
                string reason = string.IsNullOrEmpty(mute.Reason) ? plugin.Translate("ReasonUnspecified") : mute.Reason;
                string duration = mute.Duration == 0 ? plugin.Translate("Permanent") : DurationParser.FormatDuration(mute.RemainingSeconds);
                UnturnedChat.Say(player.playerID.steamID, plugin.Translate("MuteMessage", reason, duration), plugin.MessageColor);
            }
        }

        private void OnRelayVoice(PlayerVoice speaker, bool wantsToUseWalkieTalkie, ref bool shouldAllow, ref bool shouldBroadcastOverRadio, ref PlayerVoice.RelayVoiceCullingHandler cullingHandler)
        {
            if (speaker == null || speaker.player == null) return;
            ulong steamId = speaker.player.channel.owner.playerID.steamID.m_SteamID;
            var mute = databaseService.GetActiveMute(steamId);
            if (mute != null && !mute.IsExpired)
            {
                shouldAllow = false;
                if (config.EnableVoiceChatMute)
                {
                    speaker.ServerSetPermissions(speaker.GetAllowTalkingWhileDead(), false);
                    if (ShouldThrottleVoice(steamId))
                    {
                        string reason = string.IsNullOrEmpty(mute.Reason) ? plugin.Translate("ReasonUnspecified") : mute.Reason;
                        string duration = mute.Duration == 0 ? plugin.Translate("Permanent") : DurationParser.FormatDuration(mute.RemainingSeconds);
                        UnturnedChat.Say(speaker.player.channel.owner.playerID.steamID, plugin.Translate("MuteVoice", reason, duration), plugin.MessageColor);
                    }
                }
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
            if (!config.EnableBlockCommands || config.BlockCommands == null || player == null || player is ConsolePlayer)
                return;

            if (ulong.TryParse(player.Id, out ulong steamId))
            {
                var mute = databaseService.GetActiveMute(steamId);
                if (mute != null && !mute.IsExpired)
                {
                    if (config.BlockCommands.Exists(c => string.Equals(c, command.Name, StringComparison.OrdinalIgnoreCase)))
                    {
                        cancel = true;
                        string reason = string.IsNullOrEmpty(mute.Reason) ? plugin.Translate("ReasonUnspecified") : mute.Reason;
                        string duration = mute.Duration == 0 ? plugin.Translate("Permanent") : DurationParser.FormatDuration(mute.RemainingSeconds);
                        UnturnedChat.Say(player, plugin.Translate("MuteBlockCommand", reason, duration), plugin.MessageColor);
                    }
                }
            }
        }

        private void CheckExpiredMutes(object state)
        {
            try
            {
                var mutes = databaseService.GetActiveMutes();
                foreach (var mute in mutes)
                {
                    if (mute.IsExpired)
                    {
                        databaseService.RemoveMute(mute.SteamID, "Console");
                        UnturnedPlayer online = UnturnedPlayer.FromCSteamID(new CSteamID(mute.SteamID));
                        if (online != null)
                        {
                            online.Player.voice.ServerSetPermissions(online.Player.voice.GetAllowTalkingWhileDead(), true);
                            UnturnedChat.Say(online, plugin.Translate("UnmuteExpired", mute.SteamName ?? online.CharacterName), plugin.MessageColor);
                        }

                        if (config.EnableUnmuteWebhooks)
                        {
                            var placeholders = new Dictionary<string, string>
                            {
                                ["name"] = mute.SteamName ?? mute.SteamID.ToString(),
                                ["steamid"] = mute.SteamID.ToString(),
                                ["reason"] = mute.Reason ?? "Unspecified",
                                ["punisher_name"] = mute.PunisherName ?? "Console",
                                ["punisher_steamid"] = mute.PunisherSteamID.ToString(),
                                ["server_name"] = Provider.serverName,
                                ["server_icon"] = plugin.Configuration.Instance.Commands.MessageIconUrl,
                                ["avatar_url"] = plugin.Configuration.Instance.Commands.MessageIconUrl
                            };
                            discordService.SendWebhookList(plugin.Configuration.Instance.DiscordService.MuteExpiredWebhookMessages, placeholders);
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
