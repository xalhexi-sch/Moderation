using System;
using System.Collections.Generic;

namespace Moderation.Services
{
    public class CooldownService
    {
        private readonly CooldownServiceConfiguration config;
        private readonly Dictionary<string, DateTime> cooldowns = new Dictionary<string, DateTime>();
        private readonly object syncLock = new object();

        public CooldownService(CooldownServiceConfiguration config)
        {
            this.config = config;
        }

        public bool CheckCooldown(ulong steamId, string key, out double remainingSeconds)
        {
            remainingSeconds = 0;
            if (config?.Cooldowns == null) return true;

            var keyConfig = config.Cooldowns.Find(c => string.Equals(c.Key, key, StringComparison.OrdinalIgnoreCase));
            if (keyConfig == null || keyConfig.Cooldown <= 0) return true;

            string mapKey = $"{steamId}_{key.ToLowerInvariant()}";
            lock (syncLock)
            {
                if (cooldowns.TryGetValue(mapKey, out DateTime expiry))
                {
                    if (DateTime.UtcNow < expiry)
                    {
                        remainingSeconds = Math.Ceiling((expiry - DateTime.UtcNow).TotalSeconds);
                        return false;
                    }
                }
                cooldowns[mapKey] = DateTime.UtcNow.AddSeconds(keyConfig.Cooldown);
            }
            return true;
        }
    }
}
