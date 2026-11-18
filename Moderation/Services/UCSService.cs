using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Moderation.Utils;
using Newtonsoft.Json;
using Rocket.Core.Logging;
using SDG.Unturned;

namespace Moderation.Services
{
    public class UCSService
    {
        private static readonly HttpClient HttpClient = new HttpClient();
        private readonly UCSServiceConfiguration config;
        private readonly ModerationPlugin plugin;

        public UCSService(ModerationPlugin plugin, UCSServiceConfiguration config)
        {
            this.plugin = plugin;
            this.config = config;
            if (config != null && config.TimeoutMiliseconds > 0)
            {
                HttpClient.Timeout = TimeSpan.FromMilliseconds(config.TimeoutMiliseconds);
            }
        }

        public bool CheckUCSBan(SteamPlayerID playerId, uint remoteIp, out string banMessage)
        {
            banMessage = null;
            if (config == null || !config.Enabled || string.IsNullOrWhiteSpace(config.APIKey) || config.APIKey == "APIKey")
            {
                return false;
            }

            try
            {
                string ipStr = config.CheckIPs ? Parser.getIPFromUInt32(remoteIp) : string.Empty;
                string hwidStr = config.CheckHWIDs ? PlayerUtil.GetHwidsString(playerId) : string.Empty;

                var payload = new
                {
                    api_key = config.APIKey,
                    server_name = config.ServerName,
                    steam_id = playerId.steamID.m_SteamID.ToString(),
                    ip = ipStr,
                    hwid = hwidStr
                };

                // Asynchronous check within timeout
                var task = Task.Run(async () =>
                {
                    using (var content = new StringContent(JsonConvert.SerializeObject(payload), System.Text.Encoding.UTF8, "application/json"))
                    {
                        var response = await HttpClient.PostAsync("https://api.unturnedcommunity.com/v1/bans/check", content);
                        if (response.IsSuccessStatusCode)
                        {
                            string json = await response.Content.ReadAsStringAsync();
                            var res = Newtonsoft.Json.Linq.JObject.Parse(json);
                            if (res != null && (bool?)res["banned"] == true)
                            {
                                string banId = (string)res["ban_id"] ?? "0";
                                string appealUrl = (string)res["appeal_url"] ?? "https://unturnedcommunity.com";
                                return (true, banId, appealUrl);
                            }
                        }
                    }
                    return (false, string.Empty, string.Empty);
                });

                if (task.Wait(config.TimeoutMiliseconds > 0 ? config.TimeoutMiliseconds : 5000))
                {
                    var result = task.Result;
                    if (result.Item1)
                    {
                        banMessage = plugin.Translate("UCSBanMessage", result.Item2, result.Item3);
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogWarning($"[Moderation] UCS ban check error: {ex.Message}");
            }

            return false;
        }
    }
}
