using System;
using System.Collections.Generic;
using Rocket.Unturned.Player;
using SDG.Unturned;
using Steamworks;

namespace Moderation.Utils
{
    public static class PlayerUtil
    {
        public static UnturnedPlayer FindPlayer(string search)
        {
            if (string.IsNullOrWhiteSpace(search))
                return null;

            if (ulong.TryParse(search, out ulong steamId))
            {
                UnturnedPlayer p = UnturnedPlayer.FromCSteamID(new CSteamID(steamId));
                if (p != null) return p;
            }

            return UnturnedPlayer.FromName(search);
        }

        public static bool TryParseSteamID(string input, out ulong steamId)
        {
            steamId = 0;
            if (string.IsNullOrWhiteSpace(input))
                return false;

            if (ulong.TryParse(input, out steamId) && steamId > 76561190000000000)
                return true;

            UnturnedPlayer p = UnturnedPlayer.FromName(input);
            if (p != null)
            {
                steamId = p.CSteamID.m_SteamID;
                return true;
            }

            return false;
        }

        public static string GetHwidsString(SteamPlayerID playerID)
        {
            if (playerID == null) return string.Empty;
            IEnumerable<byte[]> hwids = playerID.GetHwids();
            if (hwids == null) return string.Empty;

            List<string> hexHwids = new List<string>();
            foreach (byte[] hwid in hwids)
            {
                if (hwid != null && hwid.Length > 0)
                {
                    hexHwids.Add(BitConverter.ToString(hwid).Replace("-", ""));
                }
            }

            return string.Join(";", hexHwids);
        }

        public static string GetIPString(SteamPlayer steamPlayer)
        {
            if (steamPlayer == null) return string.Empty;
            try
            {
                string addr = steamPlayer.getAddressString(false);
                return addr ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}
