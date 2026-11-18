using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using Moderation.Models;
using MySqlConnector;
using Rocket.Core.Logging;

namespace Moderation.Services
{
    public class DatabaseService
    {
        private readonly DatabaseConfiguration config;
        private readonly List<BanRecord> cachedBans = new List<BanRecord>();
        private readonly List<MuteRecord> cachedMutes = new List<MuteRecord>();
        private readonly Dictionary<ulong, PlayerRecord> cachedPlayers = new Dictionary<ulong, PlayerRecord>();
        private readonly List<PunishmentRecord> cachedPunishments = new List<PunishmentRecord>();
        private readonly object syncLock = new object();
        private bool isConnected = false;

        public DatabaseService(DatabaseConfiguration config)
        {
            this.config = config;
        }

        private string ConnectionString =>
            $"Server={config.MySQLAddress};Port={config.MySQLDatabasePort};Database={config.MySQLDatabase};Uid={config.MySQLUsername};Pwd={config.MySQLPassword};AllowUserVariables=True;UseAffectedRows=False;";

        public void Initialize()
        {
            ThreadPool.QueueUserWorkItem(_ =>
            {
                try
                {
                    using (var connection = new MySqlConnection(ConnectionString))
                    {
                        connection.Open();
                        CreateTables(connection);
                        isConnected = true;
                    }
                    ReloadCache();
                    Logger.Log("[Moderation] Successfully connected to MySQL database.");
                }
                catch (Exception ex)
                {
                    isConnected = false;
                    Logger.LogWarning($"[Moderation] Could not connect to MySQL database: {ex.Message}. Running with in-memory storage.");
                }
            });
        }

        private void CreateTables(MySqlConnection connection)
        {
            string query = $@"
CREATE TABLE IF NOT EXISTS `{config.PlayersTableName}` (
    `SteamID` VARCHAR(32) NOT NULL,
    `SteamName` VARCHAR(64) DEFAULT NULL,
    `IP` VARCHAR(45) DEFAULT NULL,
    `HWID` TEXT DEFAULT NULL,
    `Country` VARCHAR(64) DEFAULT NULL,
    `LastOnline` DATETIME NOT NULL,
    PRIMARY KEY (`SteamID`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS `{config.BansTableName}` (
    `Id` INT AUTO_INCREMENT NOT NULL,
    `SteamID` VARCHAR(32) NOT NULL,
    `SteamName` VARCHAR(64) DEFAULT NULL,
    `IP` VARCHAR(45) DEFAULT NULL,
    `HWID` TEXT DEFAULT NULL,
    `Reason` VARCHAR(255) DEFAULT NULL,
    `Duration` BIGINT NOT NULL,
    `PunisherSteamID` VARCHAR(32) NOT NULL,
    `PunisherName` VARCHAR(64) DEFAULT NULL,
    `CreatedAt` DATETIME NOT NULL,
    `ExpiresAt` DATETIME DEFAULT NULL,
    `IsActive` TINYINT(1) NOT NULL DEFAULT 1,
    `UnbannedBy` VARCHAR(32) DEFAULT NULL,
    `UnbannedAt` DATETIME DEFAULT NULL,
    PRIMARY KEY (`Id`),
    KEY `idx_steam` (`SteamID`),
    KEY `idx_active` (`IsActive`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS `{config.MutesTableName}` (
    `Id` INT AUTO_INCREMENT NOT NULL,
    `SteamID` VARCHAR(32) NOT NULL,
    `SteamName` VARCHAR(64) DEFAULT NULL,
    `Reason` VARCHAR(255) DEFAULT NULL,
    `Duration` BIGINT NOT NULL,
    `PunisherSteamID` VARCHAR(32) NOT NULL,
    `PunisherName` VARCHAR(64) DEFAULT NULL,
    `CreatedAt` DATETIME NOT NULL,
    `ExpiresAt` DATETIME DEFAULT NULL,
    `IsActive` TINYINT(1) NOT NULL DEFAULT 1,
    `UnmutedBy` VARCHAR(32) DEFAULT NULL,
    `UnmutedAt` DATETIME DEFAULT NULL,
    PRIMARY KEY (`Id`),
    KEY `idx_steam` (`SteamID`),
    KEY `idx_active` (`IsActive`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS `{config.PunishmentsTableName}` (
    `Id` INT AUTO_INCREMENT NOT NULL,
    `Type` VARCHAR(32) NOT NULL,
    `SteamID` VARCHAR(32) NOT NULL,
    `Reason` VARCHAR(255) DEFAULT NULL,
    `PunisherSteamID` VARCHAR(32) NOT NULL,
    `PunisherName` VARCHAR(64) DEFAULT NULL,
    `CreatedAt` DATETIME NOT NULL,
    PRIMARY KEY (`Id`),
    KEY `idx_steam` (`SteamID`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
";
            using (var cmd = new MySqlCommand(query, connection))
            {
                cmd.ExecuteNonQuery();
            }
        }

        public void ReloadCache()
        {
            if (!isConnected) return;
            try
            {
                using (var connection = new MySqlConnection(ConnectionString))
                {
                    connection.Open();

                    // Load active bans
                    string banQuery = $"SELECT * FROM `{config.BansTableName}` WHERE `IsActive` = 1";
                    using (var cmd = new MySqlCommand(banQuery, connection))
                    using (var reader = cmd.ExecuteReader())
                    {
                        var list = new List<BanRecord>();
                        while (reader.Read())
                        {
                            list.Add(new BanRecord
                            {
                                Id = reader.GetInt32("Id"),
                                SteamID = ulong.Parse(reader.GetString("SteamID")),
                                SteamName = reader.IsDBNull(reader.GetOrdinal("SteamName")) ? null : reader.GetString("SteamName"),
                                IP = reader.IsDBNull(reader.GetOrdinal("IP")) ? null : reader.GetString("IP"),
                                HWID = reader.IsDBNull(reader.GetOrdinal("HWID")) ? null : reader.GetString("HWID"),
                                Reason = reader.IsDBNull(reader.GetOrdinal("Reason")) ? null : reader.GetString("Reason"),
                                Duration = (uint)reader.GetInt64("Duration"),
                                PunisherSteamID = ulong.Parse(reader.GetString("PunisherSteamID")),
                                PunisherName = reader.IsDBNull(reader.GetOrdinal("PunisherName")) ? null : reader.GetString("PunisherName"),
                                CreatedAt = reader.GetDateTime("CreatedAt"),
                                ExpiresAt = reader.IsDBNull(reader.GetOrdinal("ExpiresAt")) ? (DateTime?)null : reader.GetDateTime("ExpiresAt"),
                                IsActive = reader.GetBoolean("IsActive")
                            });
                        }
                        lock (syncLock)
                        {
                            cachedBans.Clear();
                            cachedBans.AddRange(list);
                        }
                    }

                    // Load active mutes
                    string muteQuery = $"SELECT * FROM `{config.MutesTableName}` WHERE `IsActive` = 1";
                    using (var cmd = new MySqlCommand(muteQuery, connection))
                    using (var reader = cmd.ExecuteReader())
                    {
                        var list = new List<MuteRecord>();
                        while (reader.Read())
                        {
                            list.Add(new MuteRecord
                            {
                                Id = reader.GetInt32("Id"),
                                SteamID = ulong.Parse(reader.GetString("SteamID")),
                                SteamName = reader.IsDBNull(reader.GetOrdinal("SteamName")) ? null : reader.GetString("SteamName"),
                                Reason = reader.IsDBNull(reader.GetOrdinal("Reason")) ? null : reader.GetString("Reason"),
                                Duration = (uint)reader.GetInt64("Duration"),
                                PunisherSteamID = ulong.Parse(reader.GetString("PunisherSteamID")),
                                PunisherName = reader.IsDBNull(reader.GetOrdinal("PunisherName")) ? null : reader.GetString("PunisherName"),
                                CreatedAt = reader.GetDateTime("CreatedAt"),
                                ExpiresAt = reader.IsDBNull(reader.GetOrdinal("ExpiresAt")) ? (DateTime?)null : reader.GetDateTime("ExpiresAt"),
                                IsActive = reader.GetBoolean("IsActive")
                            });
                        }
                        lock (syncLock)
                        {
                            cachedMutes.Clear();
                            cachedMutes.AddRange(list);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                if (config.EnableGameThreadWarnings)
                    Logger.LogWarning($"[Moderation] Error refreshing cache from database: {ex.Message}");
            }
        }

        public void SavePlayer(PlayerRecord player)
        {
            lock (syncLock)
            {
                cachedPlayers[player.SteamID] = player;
            }

            if (!isConnected) return;
            ThreadPool.QueueUserWorkItem(_ =>
            {
                try
                {
                    using (var connection = new MySqlConnection(ConnectionString))
                    {
                        connection.Open();
                        string query = $@"
INSERT INTO `{config.PlayersTableName}` (`SteamID`, `SteamName`, `IP`, `HWID`, `Country`, `LastOnline`)
VALUES (@SteamID, @SteamName, @IP, @HWID, @Country, @LastOnline)
ON DUPLICATE KEY UPDATE
    `SteamName` = VALUES(`SteamName`),
    `IP` = VALUES(`IP`),
    `HWID` = VALUES(`HWID`),
    `Country` = VALUES(`Country`),
    `LastOnline` = VALUES(`LastOnline`);
";
                        using (var cmd = new MySqlCommand(query, connection))
                        {
                            cmd.Parameters.AddWithValue("@SteamID", player.SteamID.ToString());
                            cmd.Parameters.AddWithValue("@SteamName", player.SteamName);
                            cmd.Parameters.AddWithValue("@IP", player.IP);
                            cmd.Parameters.AddWithValue("@HWID", player.HWID);
                            cmd.Parameters.AddWithValue("@Country", player.Country);
                            cmd.Parameters.AddWithValue("@LastOnline", player.LastOnline);
                            cmd.ExecuteNonQuery();
                        }
                    }
                }
                catch (Exception ex)
                {
                    if (config.EnableGameThreadWarnings)
                        Logger.LogWarning($"[Moderation] Failed to save player: {ex.Message}");
                }
            });
        }

        public PlayerRecord GetPlayer(ulong steamId)
        {
            lock (syncLock)
            {
                if (cachedPlayers.TryGetValue(steamId, out PlayerRecord player))
                    return player;
            }

            if (!isConnected) return null;
            try
            {
                using (var connection = new MySqlConnection(ConnectionString))
                {
                    connection.Open();
                    string query = $"SELECT * FROM `{config.PlayersTableName}` WHERE `SteamID` = @SteamID LIMIT 1";
                    using (var cmd = new MySqlCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@SteamID", steamId.ToString());
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                var record = new PlayerRecord
                                {
                                    SteamID = ulong.Parse(reader.GetString("SteamID")),
                                    SteamName = reader.IsDBNull(reader.GetOrdinal("SteamName")) ? null : reader.GetString("SteamName"),
                                    IP = reader.IsDBNull(reader.GetOrdinal("IP")) ? null : reader.GetString("IP"),
                                    HWID = reader.IsDBNull(reader.GetOrdinal("HWID")) ? null : reader.GetString("HWID"),
                                    Country = reader.IsDBNull(reader.GetOrdinal("Country")) ? null : reader.GetString("Country"),
                                    LastOnline = reader.GetDateTime("LastOnline")
                                };
                                lock (syncLock)
                                {
                                    cachedPlayers[steamId] = record;
                                }
                                return record;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                if (config.EnableGameThreadWarnings)
                    Logger.LogWarning($"[Moderation] Failed to fetch player: {ex.Message}");
            }
            return null;
        }

        public void AddBan(BanRecord ban)
        {
            lock (syncLock)
            {
                cachedBans.Add(ban);
            }

            AddPunishment(new PunishmentRecord
            {
                Type = "Ban",
                SteamID = ban.SteamID,
                Reason = ban.Reason,
                PunisherSteamID = ban.PunisherSteamID,
                PunisherName = ban.PunisherName,
                CreatedAt = ban.CreatedAt
            });

            if (!isConnected) return;
            ThreadPool.QueueUserWorkItem(_ =>
            {
                try
                {
                    using (var connection = new MySqlConnection(ConnectionString))
                    {
                        connection.Open();
                        string query = $@"
INSERT INTO `{config.BansTableName}`
(`SteamID`, `SteamName`, `IP`, `HWID`, `Reason`, `Duration`, `PunisherSteamID`, `PunisherName`, `CreatedAt`, `ExpiresAt`, `IsActive`)
VALUES
(@SteamID, @SteamName, @IP, @HWID, @Reason, @Duration, @PunisherSteamID, @PunisherName, @CreatedAt, @ExpiresAt, @IsActive);
SELECT LAST_INSERT_ID();";
                        using (var cmd = new MySqlCommand(query, connection))
                        {
                            cmd.Parameters.AddWithValue("@SteamID", ban.SteamID.ToString());
                            cmd.Parameters.AddWithValue("@SteamName", ban.SteamName);
                            cmd.Parameters.AddWithValue("@IP", ban.IP);
                            cmd.Parameters.AddWithValue("@HWID", ban.HWID);
                            cmd.Parameters.AddWithValue("@Reason", ban.Reason);
                            cmd.Parameters.AddWithValue("@Duration", ban.Duration);
                            cmd.Parameters.AddWithValue("@PunisherSteamID", ban.PunisherSteamID.ToString());
                            cmd.Parameters.AddWithValue("@PunisherName", ban.PunisherName);
                            cmd.Parameters.AddWithValue("@CreatedAt", ban.CreatedAt);
                            cmd.Parameters.AddWithValue("@ExpiresAt", (object)ban.ExpiresAt ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@IsActive", ban.IsActive ? 1 : 0);

                            object res = cmd.ExecuteScalar();
                            if (res != null && int.TryParse(res.ToString(), out int id))
                            {
                                ban.Id = id;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    if (config.EnableGameThreadWarnings)
                        Logger.LogWarning($"[Moderation] Failed to insert ban: {ex.Message}");
                }
            });
        }

        public void RemoveBan(ulong steamId, string unbannedBy)
        {
            lock (syncLock)
            {
                foreach (var ban in cachedBans)
                {
                    if (ban.SteamID == steamId && ban.IsActive)
                    {
                        ban.IsActive = false;
                        ban.UnbannedBy = unbannedBy;
                        ban.UnbannedAt = DateTime.UtcNow;
                    }
                }
            }

            if (!isConnected) return;
            ThreadPool.QueueUserWorkItem(_ =>
            {
                try
                {
                    using (var connection = new MySqlConnection(ConnectionString))
                    {
                        connection.Open();
                        string query = $"UPDATE `{config.BansTableName}` SET `IsActive` = 0, `UnbannedBy` = @UnbannedBy, `UnbannedAt` = @UnbannedAt WHERE `SteamID` = @SteamID AND `IsActive` = 1";
                        using (var cmd = new MySqlCommand(query, connection))
                        {
                            cmd.Parameters.AddWithValue("@SteamID", steamId.ToString());
                            cmd.Parameters.AddWithValue("@UnbannedBy", unbannedBy);
                            cmd.Parameters.AddWithValue("@UnbannedAt", DateTime.UtcNow);
                            cmd.ExecuteNonQuery();
                        }
                    }
                }
                catch (Exception ex)
                {
                    if (config.EnableGameThreadWarnings)
                        Logger.LogWarning($"[Moderation] Failed to update ban: {ex.Message}");
                }
            });
        }

        public void AddMute(MuteRecord mute)
        {
            lock (syncLock)
            {
                cachedMutes.Add(mute);
            }

            AddPunishment(new PunishmentRecord
            {
                Type = "Mute",
                SteamID = mute.SteamID,
                Reason = mute.Reason,
                PunisherSteamID = mute.PunisherSteamID,
                PunisherName = mute.PunisherName,
                CreatedAt = mute.CreatedAt
            });

            if (!isConnected) return;
            ThreadPool.QueueUserWorkItem(_ =>
            {
                try
                {
                    using (var connection = new MySqlConnection(ConnectionString))
                    {
                        connection.Open();
                        string query = $@"
INSERT INTO `{config.MutesTableName}`
(`SteamID`, `SteamName`, `Reason`, `Duration`, `PunisherSteamID`, `PunisherName`, `CreatedAt`, `ExpiresAt`, `IsActive`)
VALUES
(@SteamID, @SteamName, @Reason, @Duration, @PunisherSteamID, @PunisherName, @CreatedAt, @ExpiresAt, @IsActive);
SELECT LAST_INSERT_ID();";
                        using (var cmd = new MySqlCommand(query, connection))
                        {
                            cmd.Parameters.AddWithValue("@SteamID", mute.SteamID.ToString());
                            cmd.Parameters.AddWithValue("@SteamName", mute.SteamName);
                            cmd.Parameters.AddWithValue("@Reason", mute.Reason);
                            cmd.Parameters.AddWithValue("@Duration", mute.Duration);
                            cmd.Parameters.AddWithValue("@PunisherSteamID", mute.PunisherSteamID.ToString());
                            cmd.Parameters.AddWithValue("@PunisherName", mute.PunisherName);
                            cmd.Parameters.AddWithValue("@CreatedAt", mute.CreatedAt);
                            cmd.Parameters.AddWithValue("@ExpiresAt", (object)mute.ExpiresAt ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@IsActive", mute.IsActive ? 1 : 0);

                            object res = cmd.ExecuteScalar();
                            if (res != null && int.TryParse(res.ToString(), out int id))
                            {
                                mute.Id = id;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    if (config.EnableGameThreadWarnings)
                        Logger.LogWarning($"[Moderation] Failed to insert mute: {ex.Message}");
                }
            });
        }

        public void RemoveMute(ulong steamId, string unmutedBy)
        {
            lock (syncLock)
            {
                foreach (var mute in cachedMutes)
                {
                    if (mute.SteamID == steamId && mute.IsActive)
                    {
                        mute.IsActive = false;
                        mute.UnmutedBy = unmutedBy;
                        mute.UnmutedAt = DateTime.UtcNow;
                    }
                }
            }

            if (!isConnected) return;
            ThreadPool.QueueUserWorkItem(_ =>
            {
                try
                {
                    using (var connection = new MySqlConnection(ConnectionString))
                    {
                        connection.Open();
                        string query = $"UPDATE `{config.MutesTableName}` SET `IsActive` = 0, `UnmutedBy` = @UnmutedBy, `UnmutedAt` = @UnmutedAt WHERE `SteamID` = @SteamID AND `IsActive` = 1";
                        using (var cmd = new MySqlCommand(query, connection))
                        {
                            cmd.Parameters.AddWithValue("@SteamID", steamId.ToString());
                            cmd.Parameters.AddWithValue("@UnmutedBy", unmutedBy);
                            cmd.Parameters.AddWithValue("@UnmutedAt", DateTime.UtcNow);
                            cmd.ExecuteNonQuery();
                        }
                    }
                }
                catch (Exception ex)
                {
                    if (config.EnableGameThreadWarnings)
                        Logger.LogWarning($"[Moderation] Failed to update mute: {ex.Message}");
                }
            });
        }

        public void AddPunishment(PunishmentRecord punishment)
        {
            lock (syncLock)
            {
                cachedPunishments.Add(punishment);
            }

            if (!isConnected) return;
            ThreadPool.QueueUserWorkItem(_ =>
            {
                try
                {
                    using (var connection = new MySqlConnection(ConnectionString))
                    {
                        connection.Open();
                        string query = $@"
INSERT INTO `{config.PunishmentsTableName}`
(`Type`, `SteamID`, `Reason`, `PunisherSteamID`, `PunisherName`, `CreatedAt`)
VALUES
(@Type, @SteamID, @Reason, @PunisherSteamID, @PunisherName, @CreatedAt);";
                        using (var cmd = new MySqlCommand(query, connection))
                        {
                            cmd.Parameters.AddWithValue("@Type", punishment.Type);
                            cmd.Parameters.AddWithValue("@SteamID", punishment.SteamID.ToString());
                            cmd.Parameters.AddWithValue("@Reason", punishment.Reason);
                            cmd.Parameters.AddWithValue("@PunisherSteamID", punishment.PunisherSteamID.ToString());
                            cmd.Parameters.AddWithValue("@PunisherName", punishment.PunisherName);
                            cmd.Parameters.AddWithValue("@CreatedAt", punishment.CreatedAt);
                            cmd.ExecuteNonQuery();
                        }
                    }
                }
                catch (Exception ex)
                {
                    if (config.EnableGameThreadWarnings)
                        Logger.LogWarning($"[Moderation] Failed to insert punishment: {ex.Message}");
                }
            });
        }

        public List<BanRecord> GetActiveBans()
        {
            lock (syncLock)
            {
                return new List<BanRecord>(cachedBans);
            }
        }

        public List<MuteRecord> GetActiveMutes()
        {
            lock (syncLock)
            {
                return new List<MuteRecord>(cachedMutes);
            }
        }

        public BanRecord GetActiveBan(ulong steamId)
        {
            lock (syncLock)
            {
                return cachedBans.Find(b => b.SteamID == steamId && b.IsActive && !b.IsExpired);
            }
        }

        public MuteRecord GetActiveMute(ulong steamId)
        {
            lock (syncLock)
            {
                return cachedMutes.Find(m => m.SteamID == steamId && m.IsActive && !m.IsExpired);
            }
        }

        public void GetPunishmentCounts(ulong steamId, out int bans, out int kicks, out int warns)
        {
            bans = 0;
            kicks = 0;
            warns = 0;

            if (isConnected)
            {
                try
                {
                    using (var connection = new MySqlConnection(ConnectionString))
                    {
                        connection.Open();
                        string query = $"SELECT `Type`, COUNT(*) as `Cnt` FROM `{config.PunishmentsTableName}` WHERE `SteamID` = @SteamID GROUP BY `Type`";
                        using (var cmd = new MySqlCommand(query, connection))
                        {
                            cmd.Parameters.AddWithValue("@SteamID", steamId.ToString());
                            using (var reader = cmd.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    string type = reader.GetString("Type");
                                    int count = reader.GetInt32("Cnt");
                                    if (string.Equals(type, "Ban", StringComparison.OrdinalIgnoreCase)) bans = count;
                                    else if (string.Equals(type, "Kick", StringComparison.OrdinalIgnoreCase)) kicks = count;
                                    else if (string.Equals(type, "Warn", StringComparison.OrdinalIgnoreCase)) warns = count;
                                }
                                return;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    if (config.EnableGameThreadWarnings)
                        Logger.LogWarning($"[Moderation] Failed to get punishment counts: {ex.Message}");
                }
            }

            lock (syncLock)
            {
                foreach (var p in cachedPunishments)
                {
                    if (p.SteamID == steamId)
                    {
                        if (string.Equals(p.Type, "Ban", StringComparison.OrdinalIgnoreCase)) bans++;
                        else if (string.Equals(p.Type, "Kick", StringComparison.OrdinalIgnoreCase)) kicks++;
                        else if (string.Equals(p.Type, "Warn", StringComparison.OrdinalIgnoreCase)) warns++;
                    }
                }
            }
        }
    }
}
