using System;
using System.Net.Http;
using System.Threading.Tasks;
using Moderation.Models;
using Moderation.Utils;
using Rocket.Core.Logging;
using Rocket.Unturned.Player;
using SDG.Unturned;

namespace Moderation.Services
{
    public class PlayerService
    {
        private static readonly HttpClient HttpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
        private readonly PlayerServiceConfiguration config;
        private readonly DatabaseService databaseService;

        public PlayerService(PlayerServiceConfiguration config, DatabaseService databaseService)
        {
            this.config = config;
            this.databaseService = databaseService;
        }

        public void OnPlayerConnected(UnturnedPlayer player)
        {
            if (player == null) return;

            Task.Run(async () =>
            {
                try
                {
                    ulong steamId = player.CSteamID.m_SteamID;
                    string steamName = player.CharacterName;
                    string ip = PlayerUtil.GetIPString(player.SteamPlayer());
                    string hwid = PlayerUtil.GetHwidsString(player.SteamPlayer().playerID);
                    string country = "Unknown";

                    if (config.ShouldSaveCountry && !string.IsNullOrEmpty(ip) && ip != "127.0.0.1" && !ip.StartsWith("192.168.") && !ip.StartsWith("10."))
                    {
                        country = await ResolveCountryAsync(ip);
                    }

                    var record = new PlayerRecord
                    {
                        SteamID = steamId,
                        SteamName = steamName,
                        IP = ip,
                        HWID = hwid,
                        Country = country,
                        LastOnline = DateTime.UtcNow
                    };

                    databaseService.SavePlayer(record);
                }
                catch (Exception ex)
                {
                    Logger.LogWarning($"[Moderation] Error recording player connection: {ex.Message}");
                }
            });
        }

        private async Task<string> ResolveCountryAsync(string ip)
        {
            try
            {
                string res = await HttpClient.GetStringAsync($"http://ip-api.com/line/{ip}?fields=country");
                if (!string.IsNullOrWhiteSpace(res))
                    return res.Trim();
            }
            catch { }
            return "Unknown";
        }
    }
}
