using System;
using System.Collections.Generic;
using System.Threading;
using Moderation.Models;
using Moderation.Utils;
using Rocket.Core.Logging;
using Rocket.Unturned.Chat;
using SDG.Unturned;

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
            this.config = config;
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
                var bans = databaseService.GetActiveBans();
                foreach (var ban in bans)
                {
                    if (ban.IsExpired)
                    {
                        databaseService.RemoveBan(ban.SteamID, "Console");

                        if (plugin.Configuration.Instance.Commands.ShouldBroadcastUnban)
                        {
                            UnturnedChat.Say(plugin.Translate("UnbanExpired", ban.SteamName ?? ban.SteamID.ToString()), plugin.MessageColor);
                        }

                        if (config.IsEnabled)
                        {
                            var placeholders = new Dictionary<string, string>
                            {
                                ["name"] = ban.SteamName ?? ban.SteamID.ToString(),
                                ["steamid"] = ban.SteamID.ToString(),
                                ["reason"] = ban.Reason ?? "Unspecified",
                                ["punisher_name"] = ban.PunisherName ?? "Console",
                                ["punisher_steamid"] = ban.PunisherSteamID.ToString(),
                                ["server_name"] = Provider.serverName,
                                ["server_icon"] = plugin.Configuration.Instance.Commands.MessageIconUrl,
                                ["avatar_url"] = plugin.Configuration.Instance.Commands.MessageIconUrl
                            };
                            discordService.SendWebhookList(plugin.Configuration.Instance.DiscordService.BanExpiredWebhookMessages, placeholders);
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
