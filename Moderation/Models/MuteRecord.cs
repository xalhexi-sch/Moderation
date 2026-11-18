using System;

namespace Moderation.Models
{
    public class MuteRecord
    {
        public int Id { get; set; }
        public ulong SteamID { get; set; }
        public string SteamName { get; set; }
        public string Reason { get; set; }
        public uint Duration { get; set; } // in seconds, 0 = permanent
        public ulong PunisherSteamID { get; set; }
        public string PunisherName { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public bool IsActive { get; set; }
        public string UnmutedBy { get; set; }
        public DateTime? UnmutedAt { get; set; }

        public bool IsExpired => Duration > 0 && ExpiresAt.HasValue && DateTime.UtcNow >= ExpiresAt.Value;

        public uint RemainingSeconds
        {
            get
            {
                if (Duration == 0) return 0;
                if (!ExpiresAt.HasValue) return 0;
                TimeSpan diff = ExpiresAt.Value - DateTime.UtcNow;
                return diff.TotalSeconds > 0 ? (uint)diff.TotalSeconds : 0;
            }
        }
    }
}
