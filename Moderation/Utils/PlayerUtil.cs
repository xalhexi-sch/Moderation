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

            if (ulong.TryParse(input, out steamId) && (steamId > 76561190000000000 || input.Length == 17))
                return true;

            UnturnedPlayer p = UnturnedPlayer.FromName(input);
            if (p != null)
            {
                steamId = p.CSteamID.m_SteamID;
                return true;
            }

            return false;
        }

        public static string GetIP(UnturnedPlayer player)
        {
            if (player == null) return string.Empty;
            try
            {
                if (player.Player != null && player.Player.channel != null && player.Player.channel.owner != null)
                {
                    string addr = GetIPString(player.Player.channel.owner);
                    if (!string.IsNullOrEmpty(addr)) return addr;
                }
                return player.IP ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        public static string GetHwids(UnturnedPlayer player)
        {
            if (player == null || player.Player == null || player.Player.channel == null)
                return string.Empty;

            try
            {
                var owner = player.Player.channel.owner;
                if (owner != null)
                {
                    return GetHwidsString(owner.playerID);
                }
            }
            catch { }

            return string.Empty;
        }

        public static string GetHwidsString(SteamPlayerID playerID)
        {
            // CRITICAL: SteamPlayerID has a bugged operator == / operator != in Unturned SDK that throws NullReferenceException on null comparisons.
            // MUST use ReferenceEquals to check for null!
            if (ReferenceEquals(playerID, null)) return string.Empty;

            try
            {
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
            catch
            {
                return string.Empty;
            }
        }

        public static string GetIPString(SteamPlayer steamPlayer)
        {
            if (steamPlayer == null) return string.Empty;
            try
            {
                if (steamPlayer.transportConnection != null)
                {
                    string addr = steamPlayer.transportConnection.GetAddressString(false);
                    if (!string.IsNullOrEmpty(addr)) return addr;
                }
            }
            catch { }
            return string.Empty;
        }
    }
}
