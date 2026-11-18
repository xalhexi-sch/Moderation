using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using Rocket.API;

namespace Moderation
{
    public class ModerationConfiguration : IRocketPluginConfiguration
    {
        public CommandsConfiguration Commands { get; set; }
        public DatabaseConfiguration Database { get; set; }
        public PlayerServiceConfiguration PlayerService { get; set; }
        public UnbanServiceConfiguration UnbanService { get; set; }
        public MuteServiceConfiguration MuteService { get; set; }
        public DiscordServiceConfiguration DiscordService { get; set; }
        public CooldownServiceConfiguration CooldownService { get; set; }
        public UCSServiceConfiguration UCSService { get; set; }

        public void LoadDefaults()
        {
            Commands = new CommandsConfiguration
            {
                MessageColor = "grey",
                MessageIconUrl = "https://i.imgur.com/ceyPI5h.png",
                ShouldRequireReason = true,
                ShouldBroadcastBan = true,
                ShouldBroadcastUnban = true,
                UseHwidBans = true,
                UseIPBans = false
            };

            Database = new DatabaseConfiguration
            {
                MySQLAddress = "127.0.0.1",
                MySQLUsername = "root",
                MySQLPassword = "password123",
                MySQLDatabase = "unturned",
                MySQLDatabasePort = 3306,
                PlayersTableName = "Players",
                BansTableName = "Bans",
                MutesTableName = "Mutes",
                PunishmentsTableName = "Punishments",
                EnableGameThreadWarnings = true
            };

            PlayerService = new PlayerServiceConfiguration
            {
                ShouldSaveCountry = true
            };

            UnbanService = new UnbanServiceConfiguration
            {
                Comment = "If you have multiple servers, this should be enabled only on one of them.",
                Comment2 = "It sends unban webhooks to discord.",
                IsEnabled = false,
                RefreshTimeSeconds = 30
            };

            MuteService = new MuteServiceConfiguration
            {
                CacheTimeSeconds = 30,
                Comment = "The below EnableUnmuteWebhooks option should only be enabled on one server if you have multiple servers.",
                EnableUnmuteWebhooks = true,
                UnmuteRefreshTimeSeconds = 30,
                EnableVoiceChatMute = true,
                VoiceChatThrottleMessagesSeconds = 3,
                EnableBlockCommands = false,
                BlockCommands = new List<string> { "pm" }
            };

            DiscordService = new DiscordServiceConfiguration
            {
                UseWebhooks = true,
                BanWebhookMessages = new List<WebhookMessage>
                {
                    new WebhookMessage
                    {
                        Enabled = false,
                        WebhookUrl = "https://discord.com/api/webhooks/123456789/xxxxxxxxxxxx",
                        Username = "Moderation",
                        AvatarUrl = "https://i.imgur.com/mhLDCUI.png",
                        Embeds = new List<DiscordEmbed>
                        {
                            new DiscordEmbed
                            {
                                Title = "{name}",
                                Url = "https://steamcommunity.com/profiles/{steamid}",
                                Description = "**{name}** was banned for **{reason}**",
                                Author = new EmbedAuthor { Name = "Player Banned", IconUrl = "https://i.imgur.com/mhLDCUI.png" },
                                Thumbnail = new EmbedThumbnail { Url = "{avatar_url}" },
                                Fields = new List<EmbedField>
                                {
                                    new EmbedField { Name = "SteamID", Value = "`{steamid}`" },
                                    new EmbedField { Name = "HWID", Value = "`{hwid}`", Inline = true },
                                    new EmbedField { Name = "IP", Value = "||{ip}||", Inline = true },
                                    new EmbedField { Name = "Duration", Value = "{duration}" },
                                    new EmbedField { Name = "Moderator", Value = "{punisher_name} ({punisher_steamid})" }
                                },
                                Footer = new EmbedFooter { Text = "{server_name}", IconUrl = "{server_icon}" },
                                WithCurrentTimestamp = true,
                                ColorHex = "ff0000"
                            }
                        }
                    }
                },
                KickWebhookMessages = new List<WebhookMessage>
                {
                    new WebhookMessage
                    {
                        Enabled = false,
                        WebhookUrl = "https://discord.com/api/webhooks/123456789/xxxxxxxxxxxx",
                        Username = "Moderation",
                        AvatarUrl = "https://i.imgur.com/mhLDCUI.png",
                        Embeds = new List<DiscordEmbed>
                        {
                            new DiscordEmbed
                            {
                                Title = "{name}",
                                Url = "https://steamcommunity.com/profiles/{steamid}",
                                Description = "**{name}** was kicked for **{reason}**",
                                Author = new EmbedAuthor { Name = "Player Kicked", IconUrl = "https://i.imgur.com/mhLDCUI.png" },
                                Thumbnail = new EmbedThumbnail { Url = "{avatar_url}" },
                                Fields = new List<EmbedField>
                                {
                                    new EmbedField { Name = "SteamID", Value = "`{steamid}`" },
                                    new EmbedField { Name = "Moderator", Value = "{punisher_name} ({punisher_steamid})" }
                                },
                                Footer = new EmbedFooter { Text = "{server_name}", IconUrl = "{server_icon}" },
                                WithCurrentTimestamp = true,
                                ColorHex = "f06c00"
                            }
                        }
                    }
                },
                WarnWebhookMessages = new List<WebhookMessage>
                {
                    new WebhookMessage
                    {
                        Enabled = false,
                        WebhookUrl = "https://discord.com/api/webhooks/123456789/xxxxxxxxxxxx",
                        Username = "Moderation",
                        AvatarUrl = "https://i.imgur.com/mhLDCUI.png",
                        Embeds = new List<DiscordEmbed>
                        {
                            new DiscordEmbed
                            {
                                Title = "{name}",
                                Url = "https://steamcommunity.com/profiles/{steamid}",
                                Description = "**{name}** was warned for **{reason}**",
                                Author = new EmbedAuthor { Name = "Player Warned", IconUrl = "https://i.imgur.com/mhLDCUI.png" },
                                Thumbnail = new EmbedThumbnail { Url = "{avatar_url}" },
                                Fields = new List<EmbedField>
                                {
                                    new EmbedField { Name = "SteamID", Value = "`{steamid}`" },
                                    new EmbedField { Name = "Moderator", Value = "{punisher_name} ({punisher_steamid})" }
                                },
                                Footer = new EmbedFooter { Text = "{server_name}", IconUrl = "{server_icon}" },
                                WithCurrentTimestamp = true,
                                ColorHex = "ffff00"
                            }
                        }
                    }
                },
                UnbanWebhookMessages = new List<WebhookMessage>
                {
                    new WebhookMessage
                    {
                        Enabled = false,
                        WebhookUrl = "https://discord.com/api/webhooks/123456789/xxxxxxxxxxxx",
                        Username = "Moderation",
                        AvatarUrl = "https://i.imgur.com/mhLDCUI.png",
                        Embeds = new List<DiscordEmbed>
                        {
                            new DiscordEmbed
                            {
                                Title = "{name}",
                                Url = "https://steamcommunity.com/profiles/{steamid}",
                                Description = "**{name}** was unbanned",
                                Author = new EmbedAuthor { Name = "Player Unbanned", IconUrl = "https://i.imgur.com/mhLDCUI.png" },
                                Thumbnail = new EmbedThumbnail { Url = "{avatar_url}" },
                                Fields = new List<EmbedField>
                                {
                                    new EmbedField { Name = "SteamID", Value = "`{steamid}`" },
                                    new EmbedField { Name = "Moderator", Value = "{punisher_name} ({punisher_steamid})" }
                                },
                                Footer = new EmbedFooter { Text = "{server_name}", IconUrl = "{server_icon}" },
                                WithCurrentTimestamp = true,
                                ColorHex = "00ff33"
                            }
                        }
                    }
                },
                MuteWebhookMessages = new List<WebhookMessage>
                {
                    new WebhookMessage
                    {
                        Enabled = false,
                        WebhookUrl = "https://discord.com/api/webhooks/123456789/xxxxxxxxxxxx",
                        Username = "Moderation",
                        AvatarUrl = "https://i.imgur.com/mhLDCUI.png",
                        Embeds = new List<DiscordEmbed>
                        {
                            new DiscordEmbed
                            {
                                Title = "{name}",
                                Url = "https://steamcommunity.com/profiles/{steamid}",
                                Description = "**{name}** was muted for **{reason}**",
                                Author = new EmbedAuthor { Name = "Player Muted", IconUrl = "https://i.imgur.com/mhLDCUI.png" },
                                Thumbnail = new EmbedThumbnail { Url = "{avatar_url}" },
                                Fields = new List<EmbedField>
                                {
                                    new EmbedField { Name = "SteamID", Value = "`{steamid}`" },
                                    new EmbedField { Name = "Duration", Value = "{duration}" },
                                    new EmbedField { Name = "Moderator", Value = "{punisher_name} ({punisher_steamid})" }
                                },
                                Footer = new EmbedFooter { Text = "{server_name}", IconUrl = "{server_icon}" },
                                WithCurrentTimestamp = true,
                                ColorHex = "ffff00"
                            }
                        }
                    }
                },
                UnmuteWebhookMessages = new List<WebhookMessage>
                {
                    new WebhookMessage
                    {
                        Enabled = false,
                        WebhookUrl = "https://discord.com/api/webhooks/123456789/xxxxxxxxxxxx",
                        Username = "Moderation",
                        AvatarUrl = "https://i.imgur.com/mhLDCUI.png",
                        Embeds = new List<DiscordEmbed>
                        {
                            new DiscordEmbed
                            {
                                Title = "{name}",
                                Url = "https://steamcommunity.com/profiles/{steamid}",
                                Description = "**{name}** was unmuted",
                                Author = new EmbedAuthor { Name = "Player Unmuted", IconUrl = "https://i.imgur.com/mhLDCUI.png" },
                                Thumbnail = new EmbedThumbnail { Url = "{avatar_url}" },
                                Fields = new List<EmbedField>
                                {
                                    new EmbedField { Name = "SteamID", Value = "`{steamid}`" },
                                    new EmbedField { Name = "Moderator", Value = "{punisher_name} ({punisher_steamid})" }
                                },
                                Footer = new EmbedFooter { Text = "{server_name}", IconUrl = "{server_icon}" },
                                WithCurrentTimestamp = true,
                                ColorHex = "00ff33"
                            }
                        }
                    }
                },
                BanExpiredWebhookMessages = new List<WebhookMessage>
                {
                    new WebhookMessage
                    {
                        Enabled = false,
                        WebhookUrl = "https://discord.com/api/webhooks/123456789/xxxxxxxxxxxx",
                        Username = "Moderation",
                        AvatarUrl = "https://i.imgur.com/mhLDCUI.png",
                        Embeds = new List<DiscordEmbed>
                        {
                            new DiscordEmbed
                            {
                                Title = "{name}",
                                Url = "https://steamcommunity.com/profiles/{steamid}",
                                Description = "**{name}**'s ban for **{reason}** has expired",
                                Author = new EmbedAuthor { Name = "Ban Expired", IconUrl = "https://i.imgur.com/mhLDCUI.png" },
                                Thumbnail = new EmbedThumbnail { Url = "{avatar_url}" },
                                Fields = new List<EmbedField>
                                {
                                    new EmbedField { Name = "SteamID", Value = "`{steamid}`" },
                                    new EmbedField { Name = "Moderator", Value = "{punisher_name} ({punisher_steamid})" }
                                },
                                Footer = new EmbedFooter { Text = "{server_name}", IconUrl = "{server_icon}" },
                                WithCurrentTimestamp = true,
                                ColorHex = "00ff33"
                            }
                        }
                    }
                },
                MuteExpiredWebhookMessages = new List<WebhookMessage>
                {
                    new WebhookMessage
                    {
                        Enabled = false,
                        WebhookUrl = "https://discord.com/api/webhooks/123456789/xxxxxxxxxxxx",
                        Username = "Moderation",
                        AvatarUrl = "https://i.imgur.com/mhLDCUI.png",
                        Embeds = new List<DiscordEmbed>
                        {
                            new DiscordEmbed
                            {
                                Title = "{name}",
                                Url = "https://steamcommunity.com/profiles/{steamid}",
                                Description = "**{name}**'s mute for **{reason}** has expired",
                                Author = new EmbedAuthor { Name = "Mute Expired", IconUrl = "https://i.imgur.com/mhLDCUI.png" },
                                Thumbnail = new EmbedThumbnail { Url = "{avatar_url}" },
                                Fields = new List<EmbedField>
                                {
                                    new EmbedField { Name = "SteamID", Value = "`{steamid}`" },
                                    new EmbedField { Name = "Moderator", Value = "{punisher_name} ({punisher_steamid})" }
                                },
                                Footer = new EmbedFooter { Text = "{server_name}", IconUrl = "{server_icon}" },
                                WithCurrentTimestamp = true,
                                ColorHex = "00ff33"
                            }
                        }
                    }
                },
                SpyWebhookMessages = new List<WebhookMessage>
                {
                    new WebhookMessage
                    {
                        Enabled = false,
                        WebhookUrl = "https://discord.com/api/webhooks/123456789/xxxxxxxxxxxx",
                        Username = "Moderation",
                        AvatarUrl = "https://i.imgur.com/mhLDCUI.png",
                        Embeds = new List<DiscordEmbed>
                        {
                            new DiscordEmbed
                            {
                                Title = "{name}",
                                Url = "https://steamcommunity.com/profiles/{steamid}",
                                Description = "**{name}** was spied",
                                Author = new EmbedAuthor { Name = "Spy", IconUrl = "https://i.imgur.com/mhLDCUI.png" },
                                Thumbnail = new EmbedThumbnail { Url = "{avatar_url}" },
                                Fields = new List<EmbedField>
                                {
                                    new EmbedField { Name = "SteamID", Value = "`{steamid}`" },
                                    new EmbedField { Name = "Moderator", Value = "{punisher_name} ({punisher_steamid})" }
                                },
                                Footer = new EmbedFooter { Text = "{server_name}", IconUrl = "{server_icon}" },
                                WithCurrentTimestamp = true,
                                ColorHex = "00ff33"
                            }
                        }
                    }
                },
                BanEvaderWebhookMessages = new List<WebhookMessage>
                {
                    new WebhookMessage
                    {
                        Enabled = false,
                        WebhookUrl = "https://discord.com/api/webhooks/123456789/xxxxxxxxxxxx",
                        Username = "Moderation",
                        AvatarUrl = "https://i.imgur.com/mhLDCUI.png",
                        Embeds = new List<DiscordEmbed>
                        {
                            new DiscordEmbed
                            {
                                Title = "{evader_name}",
                                Url = "https://steamcommunity.com/profiles/{evader_steamid}",
                                Description = "**{evader_name}** was denied connection to the server, because their **{evader_method}** matched the ban of a player **{name}** ({steamid})",
                                Author = new EmbedAuthor { Name = "Ban Evader", IconUrl = "https://i.imgur.com/mhLDCUI.png" },
                                Thumbnail = new EmbedThumbnail { Url = "{avatar_url}" },
                                Fields = new List<EmbedField>
                                {
                                    new EmbedField { Name = "Evader", Value = "{evader_name} ({evader_steamid})" },
                                    new EmbedField { Name = "Banned Player", Value = "{name} ({steamid})" },
                                    new EmbedField { Name = "Ban Reason", Value = "`{reason}`" },
                                    new EmbedField { Name = "Ban Duration", Value = "{duration} ({time_left} left)" },
                                    new EmbedField { Name = "Ban Moderator", Value = "{punisher_name} ({punisher_steamid})" }
                                },
                                Footer = new EmbedFooter { Text = "{server_name}", IconUrl = "{server_icon}" },
                                WithCurrentTimestamp = true,
                                ColorHex = "ff0000"
                            }
                        }
                    }
                }
            };

            CooldownService = new CooldownServiceConfiguration
            {
                Cooldowns = new List<CooldownKey>
                {
                    new CooldownKey { Key = "spy", Cooldown = 2 }
                }
            };

            UCSService = new UCSServiceConfiguration
            {
                Enabled = false,
                TimeoutMiliseconds = 5000,
                APIKey = "APIKey",
                ServerName = "ServerName",
                CheckHWIDs = true,
                CheckIPs = true
            };
        }
    }

    public class CommandsConfiguration
    {
        public string MessageColor { get; set; }
        public string MessageIconUrl { get; set; }
        public bool ShouldRequireReason { get; set; }
        public bool ShouldBroadcastBan { get; set; }
        public bool ShouldBroadcastUnban { get; set; }
        public bool UseHwidBans { get; set; }
        public bool UseIPBans { get; set; }
    }

    public class DatabaseConfiguration
    {
        public string MySQLAddress { get; set; }
        public string MySQLUsername { get; set; }
        public string MySQLPassword { get; set; }
        public string MySQLDatabase { get; set; }
        public int MySQLDatabasePort { get; set; }
        public string PlayersTableName { get; set; }
        public string BansTableName { get; set; }
        public string MutesTableName { get; set; }
        public string PunishmentsTableName { get; set; }
        public bool EnableGameThreadWarnings { get; set; }
    }

    public class PlayerServiceConfiguration
    {
        public bool ShouldSaveCountry { get; set; }
    }

    public class UnbanServiceConfiguration
    {
        public string Comment { get; set; }
        public string Comment2 { get; set; }
        public bool IsEnabled { get; set; }
        public int RefreshTimeSeconds { get; set; }
    }

    public class MuteServiceConfiguration
    {
        public int CacheTimeSeconds { get; set; }
        public string Comment { get; set; }
        public bool EnableUnmuteWebhooks { get; set; }
        public int UnmuteRefreshTimeSeconds { get; set; }
        public bool EnableVoiceChatMute { get; set; }
        public int VoiceChatThrottleMessagesSeconds { get; set; }
        public bool EnableBlockCommands { get; set; }

        [XmlArrayItem("BlockCommand")]
        public List<string> BlockCommands { get; set; }
    }

    public class DiscordServiceConfiguration
    {
        public bool UseWebhooks { get; set; }

        [XmlArrayItem("WebhookMessage")]
        public List<WebhookMessage> BanWebhookMessages { get; set; }

        [XmlArrayItem("WebhookMessage")]
        public List<WebhookMessage> KickWebhookMessages { get; set; }

        [XmlArrayItem("WebhookMessage")]
        public List<WebhookMessage> WarnWebhookMessages { get; set; }

        [XmlArrayItem("WebhookMessage")]
        public List<WebhookMessage> UnbanWebhookMessages { get; set; }

        [XmlArrayItem("WebhookMessage")]
        public List<WebhookMessage> MuteWebhookMessages { get; set; }

        [XmlArrayItem("WebhookMessage")]
        public List<WebhookMessage> UnmuteWebhookMessages { get; set; }

        [XmlArrayItem("WebhookMessage")]
        public List<WebhookMessage> BanExpiredWebhookMessages { get; set; }

        [XmlArrayItem("WebhookMessage")]
        public List<WebhookMessage> MuteExpiredWebhookMessages { get; set; }

        [XmlArrayItem("WebhookMessage")]
        public List<WebhookMessage> SpyWebhookMessages { get; set; }

        [XmlArrayItem("WebhookMessage")]
        public List<WebhookMessage> BanEvaderWebhookMessages { get; set; }
    }

    public class WebhookMessage
    {
        [XmlAttribute]
        public bool Enabled { get; set; }
        public string WebhookUrl { get; set; }
        public string Username { get; set; }
        public string AvatarUrl { get; set; }

        [XmlArrayItem("Embed")]
        public List<DiscordEmbed> Embeds { get; set; }
    }

    public class DiscordEmbed
    {
        public string Title { get; set; }
        public string Url { get; set; }
        public string Description { get; set; }
        public EmbedAuthor Author { get; set; }
        public EmbedThumbnail Thumbnail { get; set; }

        [XmlArrayItem("Field")]
        public List<EmbedField> Fields { get; set; }

        public EmbedFooter Footer { get; set; }
        public bool WithCurrentTimestamp { get; set; }
        public string ColorHex { get; set; }
    }

    public class EmbedAuthor
    {
        [XmlAttribute]
        public string Name { get; set; }
        [XmlAttribute]
        public string IconUrl { get; set; }
    }

    public class EmbedThumbnail
    {
        [XmlAttribute]
        public string Url { get; set; }
    }

    public class EmbedFooter
    {
        [XmlAttribute]
        public string Text { get; set; }
        [XmlAttribute]
        public string IconUrl { get; set; }
    }

    public class EmbedField
    {
        [XmlAttribute]
        public string Name { get; set; }
        [XmlAttribute]
        public string Value { get; set; }
        [XmlAttribute]
        public bool Inline { get; set; }
    }

    public class CooldownServiceConfiguration
    {
        [XmlArrayItem("CooldownKey")]
        public List<CooldownKey> Cooldowns { get; set; }
    }

    public class CooldownKey
    {
        [XmlAttribute]
        public string Key { get; set; }
        [XmlAttribute]
        public int Cooldown { get; set; }
    }

    public class UCSServiceConfiguration
    {
        [XmlAttribute]
        public bool Enabled { get; set; }
        public int TimeoutMiliseconds { get; set; }
        public string APIKey { get; set; }
        public string ServerName { get; set; }
        public bool CheckHWIDs { get; set; }
        public bool CheckIPs { get; set; }
    }
}
