using System;
using System.Net.Http;
using System.Threading.Tasks;
using Moderation.Utils;
using Newtonsoft.Json;
using Rocket.Core.Logging;
using Rocket.Core.Utils;
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
            this.config = config ?? new UCSServiceConfiguration();
            if (this.config.TimeoutMiliseconds > 0)
            {
                HttpClient.Timeout = TimeSpan.FromMilliseconds(this.config.TimeoutMiliseconds);
            }
        }

        public bool CheckUCSBan(SteamPlayerID playerId, uint remoteIp, out string banMessage)
        {
            banMessage = null;
            if (ReferenceEquals(playerId, null) || config == null || !config.Enabled || string.IsNullOrWhiteSpace(config.APIKey) || config.APIKey == "APIKey")
            {
                return false;
            }

            try
            {
                string ipStr = config.CheckIPs ? Parser.getIPFromUInt32(remoteIp) : string.Empty;
                string hwidStr = config.CheckHWIDs ? PlayerUtil.GetHwidsString(playerId) : string.Empty;
                ulong steamId = playerId.steamID.m_SteamID;

                var payload = new
                {
                    api_key = config.APIKey,
                    server_name = config.ServerName,
                    steam_id = steamId.ToString(),
                    ip = ipStr,
                    hwid = hwidStr
                };

                // Non-blocking async check with capped handshake wait (max 400ms) to prevent server thread starvation
                var checkTask = Task.Run(async () =>
                {
                    try
                    {
                        using (var content = new StringContent(JsonConvert.SerializeObject(payload), System.Text.Encoding.UTF8, "application/json"))
                        {
                            var response = await HttpClient.PostAsync("https://api.unturnedcommunity.com/v1/bans/check", content).ConfigureAwait(false);
                            if (response.IsSuccessStatusCode)
                            {
                                string json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                                var res = Newtonsoft.Json.Linq.JObject.Parse(json);
                                if (res != null && (bool?)res["banned"] == true)
                                {
                                    string banId = (string)res["ban_id"] ?? "0";
                                    string appealUrl = (string)res["appeal_url"] ?? "https://unturnedcommunity.com";
                                    return (true, banId, appealUrl);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.LogWarning($"[Moderation] Async UCS check request error: {ex.Message}");
                    }
                    return (false, string.Empty, string.Empty);
                });

                int handshakeWait = Math.Min(config.TimeoutMiliseconds > 0 ? config.TimeoutMiliseconds : 400, 400);
                if (checkTask.Wait(handshakeWait))
                {
                    var result = checkTask.Result;
                    if (result.Item1)
                    {
                        banMessage = plugin.Translate("UCSBanMessage", result.Item2, result.Item3);
                        return true;
                    }
                }
                else
                {
                    // If handshake wait elapsed, continue waiting in background and kick if banned
                    checkTask.ContinueWith(t =>
                    {
                        if (t.IsCompleted && !t.IsFaulted && t.Result.Item1)
                        {
                            string kickMsg = plugin?.Translate("UCSBanMessage", t.Result.Item2, t.Result.Item3) ?? "Banned by UCS.";
                            TaskDispatcher.QueueOnMainThread(() =>
                            {
                                Provider.kick(playerId.steamID, kickMsg);
                            });
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                Logger.LogWarning($"[Moderation] UCS ban check exception: {ex.Message}");
            }

            return false;
        }
    }
}
