using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using Rocket.API;

namespace Moderation
{
    public class ModerationConfiguration : IRocketPluginConfiguration
    {
        public CommandsConfiguration Commands { get; set; } = new CommandsConfiguration();
        public DatabaseConfiguration Database { get; set; } = new DatabaseConfiguration();
        public PlayerServiceConfiguration PlayerService { get; set; } = new PlayerServiceConfiguration();
        public UnbanServiceConfiguration UnbanService { get; set; } = new UnbanServiceConfiguration();
        public MuteServiceConfiguration MuteService { get; set; } = new MuteServiceConfiguration();
        public DiscordServiceConfiguration DiscordService { get; set; } = new DiscordServiceConfiguration();
        public CooldownServiceConfiguration CooldownService { get; set; } = new CooldownServiceConfiguration();
        public UCSServiceConfiguration UCSService { get; set; } = new UCSServiceConfiguration();

        public ModerationConfiguration()
        {
            LoadDefaults();
        }

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
        public string MessageColor { get; set; } = "grey";
        public string MessageIconUrl { get; set; } = "https://i.imgur.com/ceyPI5h.png";
        public bool ShouldRequireReason { get; set; } = true;
        public bool ShouldBroadcastBan { get; set; } = true;
        public bool ShouldBroadcastUnban { get; set; } = true;
        public bool UseHwidBans { get; set; } = true;
        public bool UseIPBans { get; set; } = false;
    }

    public class DatabaseConfiguration
    {
        public string MySQLAddress { get; set; } = "127.0.0.1";
        public string MySQLUsername { get; set; } = "root";
        public string MySQLPassword { get; set; } = "password123";
        public string MySQLDatabase { get; set; } = "unturned";
        public int MySQLDatabasePort { get; set; } = 3306;
        public string PlayersTableName { get; set; } = "Players";
        public string BansTableName { get; set; } = "Bans";
        public string MutesTableName { get; set; } = "Mutes";
        public string PunishmentsTableName { get; set; } = "Punishments";
        public bool EnableGameThreadWarnings { get; set; } = true;
    }

    public class PlayerServiceConfiguration
    {
        public bool ShouldSaveCountry { get; set; } = true;
    }

    public class UnbanServiceConfiguration
    {
        public string Comment { get; set; } = "If you have multiple servers, this should be enabled only on one of them.";
        public string Comment2 { get; set; } = "It sends unban webhooks to discord.";
        public bool IsEnabled { get; set; } = false;
        public int RefreshTimeSeconds { get; set; } = 30;
    }

    public class MuteServiceConfiguration
    {
        public int CacheTimeSeconds { get; set; } = 30;
        public string Comment { get; set; } = "The below EnableUnmuteWebhooks option should only be enabled on one server if you have multiple servers.";
        public bool EnableUnmuteWebhooks { get; set; } = true;
        public int UnmuteRefreshTimeSeconds { get; set; } = 30;
        public bool EnableVoiceChatMute { get; set; } = true;
        public int VoiceChatThrottleMessagesSeconds { get; set; } = 3;
        public bool EnableBlockCommands { get; set; } = false;

        [XmlArrayItem("BlockCommand")]
        public List<string> BlockCommands { get; set; } = new List<string> { "pm" };
    }

    public class DiscordServiceConfiguration
    {
        public bool UseWebhooks { get; set; } = true;

        [XmlArrayItem("WebhookMessage")]
        public List<WebhookMessage> BanWebhookMessages { get; set; } = new List<WebhookMessage>();

        [XmlArrayItem("WebhookMessage")]
        public List<WebhookMessage> KickWebhookMessages { get; set; } = new List<WebhookMessage>();

        [XmlArrayItem("WebhookMessage")]
        public List<WebhookMessage> WarnWebhookMessages { get; set; } = new List<WebhookMessage>();

        [XmlArrayItem("WebhookMessage")]
        public List<WebhookMessage> UnbanWebhookMessages { get; set; } = new List<WebhookMessage>();

        [XmlArrayItem("WebhookMessage")]
        public List<WebhookMessage> MuteWebhookMessages { get; set; } = new List<WebhookMessage>();

        [XmlArrayItem("WebhookMessage")]
        public List<WebhookMessage> UnmuteWebhookMessages { get; set; } = new List<WebhookMessage>();

        [XmlArrayItem("WebhookMessage")]
        public List<WebhookMessage> BanExpiredWebhookMessages { get; set; } = new List<WebhookMessage>();

        [XmlArrayItem("WebhookMessage")]
        public List<WebhookMessage> MuteExpiredWebhookMessages { get; set; } = new List<WebhookMessage>();

        [XmlArrayItem("WebhookMessage")]
        public List<WebhookMessage> SpyWebhookMessages { get; set; } = new List<WebhookMessage>();

        [XmlArrayItem("WebhookMessage")]
        public List<WebhookMessage> BanEvaderWebhookMessages { get; set; } = new List<WebhookMessage>();
    }

    public class WebhookMessage
    {
        [XmlAttribute]
        public bool Enabled { get; set; } = false;
        public string WebhookUrl { get; set; } = "";
        public string Username { get; set; } = "Moderation";
        public string AvatarUrl { get; set; } = "";

        [XmlArrayItem("Embed")]
        public List<DiscordEmbed> Embeds { get; set; } = new List<DiscordEmbed>();
    }

    public class DiscordEmbed
    {
        public string Title { get; set; }
        public string Url { get; set; }
        public string Description { get; set; }
        public EmbedAuthor Author { get; set; }
        public EmbedThumbnail Thumbnail { get; set; }

        [XmlArrayItem("Field")]
        public List<EmbedField> Fields { get; set; } = new List<EmbedField>();

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
        public List<CooldownKey> Cooldowns { get; set; } = new List<CooldownKey>();
    }

    public class CooldownKey
    {
        [XmlAttribute]
        public string Key { get; set; } = "";
        [XmlAttribute]
        public int Cooldown { get; set; } = 0;
    }

    public class UCSServiceConfiguration
    {
        [XmlAttribute]
        public bool Enabled { get; set; } = false;
        public int TimeoutMiliseconds { get; set; } = 5000;
        public string APIKey { get; set; } = "APIKey";
        public string ServerName { get; set; } = "ServerName";
        public bool CheckHWIDs { get; set; } = true;
        public bool CheckIPs { get; set; } = true;
    }
}
