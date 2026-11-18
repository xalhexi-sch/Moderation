using System;

namespace Moderation.Models
{
    public class PlayerRecord
    {
        public ulong SteamID { get; set; }
        public string SteamName { get; set; }
        public string IP { get; set; }
        public string HWID { get; set; }
        public string Country { get; set; }
        public DateTime LastOnline { get; set; }
    }
}
