using System;
using Moderation.Services;
using Rocket.API.Collections;
using Rocket.Core.Plugins;
using Rocket.Unturned;
using Rocket.Unturned.Chat;
using Rocket.Unturned.Player;
using UnityEngine;
using Logger = Rocket.Core.Logging.Logger;

namespace Moderation
{
    public class ModerationPlugin : RocketPlugin<ModerationConfiguration>
    {
        public static ModerationPlugin Instance { get; private set; }

        public DatabaseService DatabaseService { get; private set; }
        public DiscordWebhookService DiscordWebhookService { get; private set; }
        public CooldownService CooldownService { get; private set; }
        public UCSService UCSService { get; private set; }
        public GlitchService GlitchService { get; private set; }
        public ModerationService ModerationService { get; private set; }
        public PlayerService PlayerService { get; private set; }
        public MuteService MuteService { get; private set; }
        public UnbanService UnbanService { get; private set; }

        public Color MessageColor
        {
            get
            {
                try
                {
                    string colorName = Configuration?.Instance?.Commands?.MessageColor;
                    if (!string.IsNullOrEmpty(colorName))
                    {
                        return UnturnedChat.GetColorFromName(colorName, Color.grey);
                    }
                }
                catch { }
                return Color.grey;
            }
        }

        protected override void Load()
        {
            Instance = this;

            try
            {
                if (Configuration != null && Configuration.Instance == null)
                {
                    Configuration.Instance = new ModerationConfiguration();
                    Configuration.Instance.LoadDefaults();
                }

                var config = Configuration?.Instance ?? new ModerationConfiguration();
                if (config.Commands == null) config.Commands = new CommandsConfiguration();
                if (config.Database == null) config.Database = new DatabaseConfiguration();
                if (config.PlayerService == null) config.PlayerService = new PlayerServiceConfiguration();
                if (config.UnbanService == null) config.UnbanService = new UnbanServiceConfiguration();
                if (config.MuteService == null) config.MuteService = new MuteServiceConfiguration();
                if (config.DiscordService == null) config.DiscordService = new DiscordServiceConfiguration();
                if (config.CooldownService == null) config.CooldownService = new CooldownServiceConfiguration();
                if (config.UCSService == null) config.UCSService = new UCSServiceConfiguration();

                DatabaseService = new DatabaseService(config.Database);
                DatabaseService.Initialize();

                DiscordWebhookService = new DiscordWebhookService(config.DiscordService);
                CooldownService = new CooldownService(config.CooldownService);
                UCSService = new UCSService(this, config.UCSService);
                GlitchService = new GlitchService();

                ModerationService = new ModerationService(this, DatabaseService, DiscordWebhookService, UCSService);
                ModerationService.Initialize();

                PlayerService = new PlayerService(config.PlayerService, DatabaseService);

                MuteService = new MuteService(this, config.MuteService, DatabaseService, DiscordWebhookService);
                MuteService.Initialize();

                UnbanService = new UnbanService(this, config.UnbanService, DatabaseService, DiscordWebhookService);
                UnbanService.Initialize();

                U.Events.OnPlayerConnected += OnPlayerConnected;

                Logger.Log($"[Moderation] {Name} {Assembly.GetName().Version} loaded successfully.");
                Logger.Log("[Moderation] Credits to Sun Beam and Soer for mutes update.");
            }
            catch (Exception ex)
            {
                Logger.LogWarning($"[Moderation] Exception in Load(): {ex}");
            }
        }

        protected override void Unload()
        {
            try
            {
                U.Events.OnPlayerConnected -= OnPlayerConnected;

                MuteService?.Shutdown();
                UnbanService?.Shutdown();
                ModerationService?.Shutdown();

                Instance = null;
                Logger.Log($"[Moderation] {Name} unloaded.");
            }
            catch (Exception ex)
            {
                Logger.LogWarning($"[Moderation] Exception in Unload(): {ex}");
            }
        }

        private void OnPlayerConnected(UnturnedPlayer player)
        {
            try
            {
                PlayerService?.OnPlayerConnected(player);
                MuteService?.OnPlayerConnected(player);
            }
            catch (Exception ex)
            {
                Logger.LogWarning($"[Moderation] Exception in OnPlayerConnected: {ex.Message}");
            }
        }

        public override TranslationList DefaultTranslations => new TranslationList
        {
            { "BanBroadcast", "{0} was banned by {1} for {2} for {3}!" },
            { "BanMessage", "[BAN] Reason: {0} Time Left: {1}" },
            { "BansLimit", "You can't view the list of all bans in-game, specify player or use console" },
            { "BansLine", "BanID: {0} SteamName: {1} SteamID: {2} PunisherName: {3} PunisherId: {4}" },
            { "BansNone", "There are no bans" },
            { "UnbanBroadcast", "{0} was unbanned!" },
            { "UnbanExpired", "{0} ban expired!" },
            { "UnbanFail", "{0} doesn't have any active ban." },
            { "KickAnnouncement", "{0} was kicked by {1} for {2}" },
            { "WarnAnnouncement", "{0} was warned by {1} for {2}" },
            { "CheckownerFail", "You are not looking at any barricade, structure or locked vehicle." },
            { "CheckownerOwnerName", "The owner of this {0} is {1} (banned: {2})" },
            { "CheckownerOwnerSteamId", "Steam ID: {0}" },
            { "CheckownerLastActive", "{0} was last active on {1}" },
            { "RequireReason", "You must specify a reason!" },
            { "PlayerInfoMessage", "{0} from {1} has {2} bans, {3} kicks & {4} warns" },
            { "Permanent", "permanent" },
            { "PlayerNotFound", "Player not found!" },
            { "KickInvalid", "You must specify target player!" },
            { "WarnInvalid", "You must specify target player!" },
            { "BanInvalid", "Ban format: /ban <player> [reason] [duration]" },
            { "UnbanNotBanned", "{0} doesnt have any active bans!" },
            { "UnbanInvalid", "You must specify player steamID!" },
            { "PlayerInfoInvalid", "You must specify target player!" },
            { "HWIDBanDisabled", "HWID bans are disabled" },
            { "IPBanDisabled", "IP bans are disabled" },
            { "UCSDisabled", "UCS service is disabled or API Key is invalid" },
            { "ReasonUnspecified", "Unspecified" },
            { "SpyNoPermission", "You don't have permission to spy" },
            { "SpyFail", "Spy format: /spy <player>" },
            { "SpySuccess", "Successfully spied {0}!" },
            { "SpyCooldown", "You have to wait {0} before you can spy again" },
            { "MuteAnnouncement", "{0} was muted by {1} for {2} for {3}!" },
            { "MuteMessage", "You cannot use chat, because you are muted for {0} for {1}" },
            { "MutesLimit", "You can't view the list of all mutes in-game, specify player or use console" },
            { "MutesNone", "There are no mutes" },
            { "MutesLine", "MuteID: {0} SteamName: {1} SteamID: {2} PunisherName: {3} PunisherId: {4}" },
            { "MuteInvalid", "Mute format: /mute <player> [reason] [duration]" },
            { "UnmuteInvalid", "You must specify player name or steamID!" },
            { "UnmuteNotMuted", "{0} doesnt have any active mutes!" },
            { "UnmuteAnnouncement", "{0} was unmuted!" },
            { "UnmuteExpired", "{0} mute expired!" },
            { "MuteBlockCommand", "You cannot execute this command, while you are muted for {0} for {1}" },
            { "MuteVoice", "You cannot use voice chat, because you are muted for {0} for {1}" },
            { "UCSBanMessage", "[UCS] You are banned. ID: #{0}. Appeal at {1}" },
            { "BanAlreadyExistsPermanent", "{0} ({1}) is already permanently banned for {2}" },
            { "BanAlreadyExists", "{0} ({1}) is already banned for {2} for a longer period of {3}" },
            { "NoGlitchesFound", "Could not find any suspected glitch objects on the map." },
            { "GlitchedDestroyed", "The glitch ({0}/{1}) was already destroyed. Use command again for next glitch." },
            { "GlitchTeleported", "Teleported to suspected glitch ({0}/{1}). Use command again for next glitch." },
            { "GlitchRescan", "Rescanned for glitches. Found {0} suspected objects." },
            { "GlitchInvalidIndex", "Invalid glitch index {0}. Valid range is 1-{1}." }
        };
    }
}
