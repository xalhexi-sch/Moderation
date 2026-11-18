using System;

namespace Moderation.Models
{
    public class PunishmentRecord
    {
        public int Id { get; set; }
        public string Type { get; set; } // "Ban", "Mute", "Kick", "Warn"
        public ulong SteamID { get; set; }
        public string Reason { get; set; }
        public ulong PunisherSteamID { get; set; }
        public string PunisherName { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
