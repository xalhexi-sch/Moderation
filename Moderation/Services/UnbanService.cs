using System;
using System.Collections.Generic;
using System.Threading;
using Moderation.Models;
using Rocket.Unturned.Chat;
using SDG.Unturned;
using UnityEngine;
using Logger = Rocket.Core.Logging.Logger;

namespace Moderation.Services
{
    public class UnbanService
    {
        private readonly ModerationPlugin plugin;
        private readonly UnbanServiceConfiguration config;
        private readonly DatabaseService databaseService;
        private readonly DiscordWebhookService discordService;
        private Timer unbanTimer;

        public UnbanService(ModerationPlugin plugin, UnbanServiceConfiguration config, DatabaseService databaseService, DiscordWebhookService discordService)
        {
            this.plugin = plugin;
            this.config = config ?? new UnbanServiceConfiguration();
            this.databaseService = databaseService;
            this.discordService = discordService;
        }

        public void Initialize()
        {
            int intervalMs = Math.Max(5, config.RefreshTimeSeconds) * 1000;
            unbanTimer = new Timer(CheckExpiredBans, null, intervalMs, intervalMs);
        }

        public void Shutdown()
        {
            unbanTimer?.Dispose();
            unbanTimer = null;
        }

        private void CheckExpiredBans(object state)
        {
            try
            {
                var bans = databaseService?.GetActiveBans();
                if (bans == null) return;

                foreach (var ban in bans)
                {
                    if (ban != null && ban.IsExpired)
                    {
                        databaseService?.RemoveBan(ban.SteamID, "Console");

                        if (plugin?.Configuration?.Instance?.Commands?.ShouldBroadcastUnban == true)
                        {
                            UnturnedChat.Say(plugin.Translate("UnbanExpired", ban.SteamName ?? ban.SteamID.ToString()), plugin?.MessageColor ?? Color.grey);
                        }

                        if (config.IsEnabled && discordService != null)
                        {
                            var placeholders = new Dictionary<string, string>
                            {
                                ["name"] = ban.SteamName ?? ban.SteamID.ToString(),
                                ["steamid"] = ban.SteamID.ToString(),
                                ["reason"] = ban.Reason ?? "Unspecified",
                                ["punisher_name"] = ban.PunisherName ?? "Console",
                                ["punisher_steamid"] = ban.PunisherSteamID.ToString(),
                                ["server_name"] = Provider.serverName ?? "Unturned Server",
                                ["server_icon"] = plugin?.Configuration?.Instance?.Commands?.MessageIconUrl ?? string.Empty,
                                ["avatar_url"] = plugin?.Configuration?.Instance?.Commands?.MessageIconUrl ?? string.Empty
                            };
                            var webhooks = plugin?.Configuration?.Instance?.DiscordService?.BanExpiredWebhookMessages;
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
                Logger.LogWarning($"[Moderation] Error in CheckExpiredBans: {ex.Message}");
            }
        }
    }
}
