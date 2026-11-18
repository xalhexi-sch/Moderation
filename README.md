# Moderation
Ban, mute, kick, warn & more. With Discord webhooks.


## Features

SteamID, IP & HWID bans.
Mutes with chat & voice chat block.
Kicks & warns.
Check owner of barricade, structure or locked vehicle.
Player info with bans, kicks & warns count.
Teleport to suspected glitch barricade or structure.
Discord webhooks for bans, kicks, warns, mutes, unbans, ban evaders & more.
Integration with UCS

## Credits

Sun Beam and Soer for mutes update.

## Commands

/ban <player> [reason] [duration] – Bans specified player for duration with reason.
/unban <player> – Removes specified player latest ban.
/bans [player] – Displays a list of all or player bans.
/mute <player> [reason] [duration] – Mutes specified player for duration with reason.
/unmute <player> – Removes specified player latest mute.
/mutes [player] – Displays a list of all or player mutes.
/kick <player> [reason] – Kicks specified player for reason.
/warn <player> [reason] – Warns specified player for reason.
/checkowner – Check's the owner of barricade/structure/locked vehicle.
/playerinfo <player> – Displays player name, country and bans, kicks, warns count.
/glitch – Teleports you to suspected glitch barricade or structure.
/glitch <index> – Teleports you to specified index of suspected glitch barricade or structure.
/glitch rescan - Force a scan for suspected glitch barricades or structures.

## Permissions
```xml
<Permission Cooldown="0">ban</Permission>
<Permission Cooldown="0">unban</Permission>
<Permission Cooldown="0">bans</Permission>
<Permission Cooldown="0">mute</Permission>
<Permission Cooldown="0">unmute</Permission>
<Permission Cooldown="0">mutes</Permission>
<Permission Cooldown="0">kick</Permission>
<Permission Cooldown="0">warn</Permission>
<Permission Cooldown="0">checkowner</Permission>
<Permission Cooldown="0">playerinfo</Permission>
<Permission Cooldown="0">glitch</Permission>
```

## Configuration
```xml
<?xml version="1.0" encoding="utf-8"?>
<ModerationConfiguration xmlns:xsd="http://www.w3.org/2001/XMLSchema" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">
  <Commands>
    <MessageColor>grey</MessageColor>
    <MessageIconUrl>https://i.imgur.com/ceyPI5h.png</MessageIconUrl>
    <ShouldRequireReason>true</ShouldRequireReason>
    <ShouldBroadcastBan>true</ShouldBroadcastBan>
    <ShouldBroadcastUnban>true</ShouldBroadcastUnban>
    <UseHwidBans>true</UseHwidBans>
    <UseIPBans>false</UseIPBans>
  </Commands>
  <Database>
    <MySQLAddress>127.0.0.1</MySQLAddress>
    <MySQLUsername>root</MySQLUsername>
    <MySQLPassword>password123</MySQLPassword>
    <MySQLDatabase>unturned</MySQLDatabase>
    <MySQLDatabasePort>3306</MySQLDatabasePort>
    <PlayersTableName>Players</PlayersTableName>
    <BansTableName>Bans</BansTableName>
    <MutesTableName>Mutes</MutesTableName>
    <PunishmentsTableName>Punishments</PunishmentsTableName>
    <EnableGameThreadWarnings>true</EnableGameThreadWarnings>
  </Database>
  <PlayerService>
    <ShouldSaveCountry>true</ShouldSaveCountry>
  </PlayerService>
  <UnbanService>
    <Comment>If you have multiple servers, this should be enabled only on one of them.</Comment>
    <Comment2>It sends unban webhooks to discord.</Comment2>
    <IsEnabled>false</IsEnabled>
    <RefreshTimeSeconds>30</RefreshTimeSeconds>
  </UnbanService>
  <MuteService>
    <CacheTimeSeconds>30</CacheTimeSeconds>
    <Comment>The below EnableUnmuteWebhooks option should only be enabled on one server if you have multiple servers.</Comment>
    <EnableUnmuteWebhooks>true</EnableUnmuteWebhooks>
    <UnmuteRefreshTimeSeconds>30</UnmuteRefreshTimeSeconds>
    <EnableVoiceChatMute>true</EnableVoiceChatMute>
    <VoiceChatThrottleMessagesSeconds>3</VoiceChatThrottleMessagesSeconds>
    <EnableBlockCommands>false</EnableBlockCommands>
    <BlockCommands>
      <BlockCommand>pm</BlockCommand>
    </BlockCommands>
  </MuteService>
  <DiscordService>
    <UseWebhooks>true</UseWebhooks>
    <BanWebhookMessages>
      <WebhookMessage Enabled="false">
        <WebhookUrl>https://discord.com/api/webhooks/123456789/xxxxxxxxxxxx</WebhookUrl>
        <Username>Moderation</Username>
        <AvatarUrl>https://i.imgur.com/mhLDCUI.png</AvatarUrl>
        <Embeds>
          <Embed>
            <Title>{name}</Title>
            <Url>https://steamcommunity.com/profiles/{steamid}</Url>
            <Description>**{name}** was banned for **{reason}**</Description>
            <Author Name="Player Banned" IconUrl="https://i.imgur.com/mhLDCUI.png" />
            <Thumbnail Url="{avatar_url}" />
            <Fields>
              <Field Name="SteamID" Value="`{steamid}`" />
              <Field Name="HWID" Value="`{hwid}`" Inline="true" />
              <Field Name="IP" Value="||{ip}||" Inline="true" />
              <Field Name="Duration" Value="{duration}" />
              <Field Name="Moderator" Value="{punisher_name} ({punisher_steamid})" />
            </Fields>
            <Footer Text="{server_name}" IconUrl="{server_icon}" />
            <WithCurrentTimestamp>true</WithCurrentTimestamp>
            <ColorHex>ff0000</ColorHex>
          </Embed>
        </Embeds>
      </WebhookMessage>
    </BanWebhookMessages>
    <KickWebhookMessages>
      <WebhookMessage Enabled="false">
        <WebhookUrl>https://discord.com/api/webhooks/123456789/xxxxxxxxxxxx</WebhookUrl>
        <Username>Moderation</Username>
        <AvatarUrl>https://i.imgur.com/mhLDCUI.png</AvatarUrl>
        <Embeds>
          <Embed>
            <Title>{name}</Title>
            <Url>https://steamcommunity.com/profiles/{steamid}</Url>
            <Description>**{name}** was kicked for **{reason}**</Description>
            <Author Name="Player Kicked" IconUrl="https://i.imgur.com/mhLDCUI.png" />
            <Thumbnail Url="{avatar_url}" />
            <Fields>
              <Field Name="SteamID" Value="`{steamid}`" />
              <Field Name="Moderator" Value="{punisher_name} ({punisher_steamid})" />
            </Fields>
            <Footer Text="{server_name}" IconUrl="{server_icon}" />
            <WithCurrentTimestamp>true</WithCurrentTimestamp>
            <ColorHex>f06c00</ColorHex>
          </Embed>
        </Embeds>
      </WebhookMessage>
    </KickWebhookMessages>
    <WarnWebhookMessages>
      <WebhookMessage Enabled="false">
        <WebhookUrl>https://discord.com/api/webhooks/123456789/xxxxxxxxxxxx</WebhookUrl>
        <Username>Moderation</Username>
        <AvatarUrl>https://i.imgur.com/mhLDCUI.png</AvatarUrl>
        <Embeds>
          <Embed>
            <Title>{name}</Title>
            <Url>https://steamcommunity.com/profiles/{steamid}</Url>
            <Description>**{name}** was warned for **{reason}**</Description>
            <Author Name="Player Warned" IconUrl="https://i.imgur.com/mhLDCUI.png" />
            <Thumbnail Url="{avatar_url}" />
            <Fields>
              <Field Name="SteamID" Value="`{steamid}`" />
              <Field Name="Moderator" Value="{punisher_name} ({punisher_steamid})" />
            </Fields>
            <Footer Text="{server_name}" IconUrl="{server_icon}" />
            <WithCurrentTimestamp>true</WithCurrentTimestamp>
            <ColorHex>ffff00</ColorHex>
          </Embed>
        </Embeds>
      </WebhookMessage>
    </WarnWebhookMessages>
    <UnbanWebhookMessages>
      <WebhookMessage Enabled="false">
        <WebhookUrl>https://discord.com/api/webhooks/123456789/xxxxxxxxxxxx</WebhookUrl>
        <Username>Moderation</Username>
        <AvatarUrl>https://i.imgur.com/mhLDCUI.png</AvatarUrl>
        <Embeds>
          <Embed>
            <Title>{name}</Title>
            <Url>https://steamcommunity.com/profiles/{steamid}</Url>
            <Description>**{name}** was unbanned</Description>
            <Author Name="Player Unbanned" IconUrl="https://i.imgur.com/mhLDCUI.png" />
            <Thumbnail Url="{avatar_url}" />
            <Fields>
              <Field Name="SteamID" Value="`{steamid}`" />
              <Field Name="Moderator" Value="{punisher_name} ({punisher_steamid})" />
            </Fields>
            <Footer Text="{server_name}" IconUrl="{server_icon}" />
            <WithCurrentTimestamp>true</WithCurrentTimestamp>
            <ColorHex>00ff33</ColorHex>
          </Embed>
        </Embeds>
      </WebhookMessage>
    </UnbanWebhookMessages>
    <MuteWebhookMessages>
      <WebhookMessage Enabled="false">
        <WebhookUrl>https://discord.com/api/webhooks/123456789/xxxxxxxxxxxx</WebhookUrl>
        <Username>Moderation</Username>
        <AvatarUrl>https://i.imgur.com/mhLDCUI.png</AvatarUrl>
        <Embeds>
          <Embed>
            <Title>{name}</Title>
            <Url>https://steamcommunity.com/profiles/{steamid}</Url>
            <Description>**{name}** was muted for **{reason}**</Description>
            <Author Name="Player Muted" IconUrl="https://i.imgur.com/mhLDCUI.png" />
            <Thumbnail Url="{avatar_url}" />
            <Fields>
              <Field Name="SteamID" Value="`{steamid}`" />
              <Field Name="Duration" Value="{duration}" />
              <Field Name="Moderator" Value="{punisher_name} ({punisher_steamid})" />
            </Fields>
            <Footer Text="{server_name}" IconUrl="{server_icon}" />
            <WithCurrentTimestamp>true</WithCurrentTimestamp>
            <ColorHex>ffff00</ColorHex>
          </Embed>
        </Embeds>
      </WebhookMessage>
    </MuteWebhookMessages>
    <UnmuteWebhookMessages>
      <WebhookMessage Enabled="false">
        <WebhookUrl>https://discord.com/api/webhooks/123456789/xxxxxxxxxxxx</WebhookUrl>
        <Username>Moderation</Username>
        <AvatarUrl>https://i.imgur.com/mhLDCUI.png</AvatarUrl>
        <Embeds>
          <Embed>
            <Title>{name}</Title>
            <Url>https://steamcommunity.com/profiles/{steamid}</Url>
            <Description>**{name}** was unmuted</Description>
            <Author Name="Player Unmuted" IconUrl="https://i.imgur.com/mhLDCUI.png" />
            <Thumbnail Url="{avatar_url}" />
            <Fields>
              <Field Name="SteamID" Value="`{steamid}`" />
              <Field Name="Moderator" Value="{punisher_name} ({punisher_steamid})" />
            </Fields>
            <Footer Text="{server_name}" IconUrl="{server_icon}" />
            <WithCurrentTimestamp>true</WithCurrentTimestamp>
            <ColorHex>00ff33</ColorHex>
          </Embed>
        </Embeds>
      </WebhookMessage>
    </UnmuteWebhookMessages>
    <BanExpiredWebhookMessages>
      <WebhookMessage Enabled="false">
        <WebhookUrl>https://discord.com/api/webhooks/123456789/xxxxxxxxxxxx</WebhookUrl>
        <Username>Moderation</Username>
        <AvatarUrl>https://i.imgur.com/mhLDCUI.png</AvatarUrl>
        <Embeds>
          <Embed>
            <Title>{name}</Title>
            <Url>https://steamcommunity.com/profiles/{steamid}</Url>
            <Description>**{name}**'s ban for **{reason}** has expired</Description>
            <Author Name="Ban Expired" IconUrl="https://i.imgur.com/mhLDCUI.png" />
            <Thumbnail Url="{avatar_url}" />
            <Fields>
              <Field Name="SteamID" Value="`{steamid}`" />
              <Field Name="Moderator" Value="{punisher_name} ({punisher_steamid})" />
            </Fields>
            <Footer Text="{server_name}" IconUrl="{server_icon}" />
            <WithCurrentTimestamp>true</WithCurrentTimestamp>
            <ColorHex>00ff33</ColorHex>
          </Embed>
        </Embeds>
      </WebhookMessage>
    </BanExpiredWebhookMessages>
    <MuteExpiredWebhookMessages>
      <WebhookMessage Enabled="false">
        <WebhookUrl>https://discord.com/api/webhooks/123456789/xxxxxxxxxxxx</WebhookUrl>
        <Username>Moderation</Username>
        <AvatarUrl>https://i.imgur.com/mhLDCUI.png</AvatarUrl>
        <Embeds>
          <Embed>
            <Title>{name}</Title>
            <Url>https://steamcommunity.com/profiles/{steamid}</Url>
            <Description>**{name}**'s mute for **{reason}** has expired</Description>
            <Author Name="Mute Expired" IconUrl="https://i.imgur.com/mhLDCUI.png" />
            <Thumbnail Url="{avatar_url}" />
            <Fields>
              <Field Name="SteamID" Value="`{steamid}`" />
              <Field Name="Moderator" Value="{punisher_name} ({punisher_steamid})" />
            </Fields>
            <Footer Text="{server_name}" IconUrl="{server_icon}" />
            <WithCurrentTimestamp>true</WithCurrentTimestamp>
            <ColorHex>00ff33</ColorHex>
          </Embed>
        </Embeds>
      </WebhookMessage>
    </MuteExpiredWebhookMessages>
    <SpyWebhookMessages>
      <WebhookMessage Enabled="false">
        <WebhookUrl>https://discord.com/api/webhooks/123456789/xxxxxxxxxxxx</WebhookUrl>
        <Username>Moderation</Username>
        <AvatarUrl>https://i.imgur.com/mhLDCUI.png</AvatarUrl>
        <Embeds>
          <Embed>
            <Title>{name}</Title>
            <Url>https://steamcommunity.com/profiles/{steamid}</Url>
            <Description>**{name}** was spied</Description>
            <Author Name="Spy" IconUrl="https://i.imgur.com/mhLDCUI.png" />
            <Thumbnail Url="{avatar_url}" />
            <Fields>
              <Field Name="SteamID" Value="`{steamid}`" />
              <Field Name="Moderator" Value="{punisher_name} ({punisher_steamid})" />
            </Fields>
            <Footer Text="{server_name}" IconUrl="{server_icon}" />
            <WithCurrentTimestamp>true</WithCurrentTimestamp>
            <ColorHex>00ff33</ColorHex>
          </Embed>
        </Embeds>
      </WebhookMessage>
    </SpyWebhookMessages>
    <BanEvaderWebhookMessages>
      <WebhookMessage Enabled="false">
        <WebhookUrl>https://discord.com/api/webhooks/123456789/xxxxxxxxxxxx</WebhookUrl>
        <Username>Moderation</Username>
        <AvatarUrl>https://i.imgur.com/mhLDCUI.png</AvatarUrl>
        <Embeds>
          <Embed>
            <Title>{evader_name}</Title>
            <Url>https://steamcommunity.com/profiles/{evader_steamid}</Url>
            <Description>**{evader_name}** was denied connection to the server, because their **{evader_method}** matched the ban of a player **{name}** ({steamid})</Description>
            <Author Name="Ban Evader" IconUrl="https://i.imgur.com/mhLDCUI.png" />
            <Thumbnail Url="{avatar_url}" />
            <Fields>
              <Field Name="Evader" Value="{evader_name} ({evader_steamid})" />
              <Field Name="Banned Player" Value="{name} ({steamid})" />
              <Field Name="Ban Reason" Value="`{reason}`" />
              <Field Name="Ban Duration" Value="{duration} ({time_left} left)" />
              <Field Name="Ban Moderator" Value="{punisher_name} ({punisher_steamid})" />
            </Fields>
            <Footer Text="{server_name}" IconUrl="{server_icon}" />
            <WithCurrentTimestamp>true</WithCurrentTimestamp>
            <ColorHex>ff0000</ColorHex>
          </Embed>
        </Embeds>
      </WebhookMessage>
    </BanEvaderWebhookMessages>
  </DiscordService>
  <CooldownService>
    <Cooldowns>
      <CooldownKey Key="spy" Cooldown="2" />
    </Cooldowns>
  </CooldownService>
  <UCSService Enabled="false">
    <TimeoutMiliseconds>5000</TimeoutMiliseconds>
    <APIKey>APIKey</APIKey>
    <ServerName>ServerName</ServerName>
    <CheckHWIDs>true</CheckHWIDs>
    <CheckIPs>true</CheckIPs>
  </UCSService>
</ModerationConfiguration>
```

## Translations
```xml
<?xml version="1.0" encoding="utf-8"?>
<Translations xmlns:xsd="http://www.w3.org/2001/XMLSchema" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">
  <Translation Id="BanBroadcast" Value="{0} was banned by {1} for {2} for {3}!" />
  <Translation Id="BanMessage" Value="[BAN] Reason: {0} Time Left: {1}" />
  <Translation Id="BansLimit" Value="You can't view the list of all bans in-game, specify player or use console" />
  <Translation Id="BansLine" Value="BanID: {0} SteamName: {1} SteamID: {2} PunisherName: {3} PunisherId: {4}" />
  <Translation Id="BansNone" Value="There are no bans" />
  <Translation Id="UnbanBroadcast" Value="{0} was unbanned!" />
  <Translation Id="UnbanExpired" Value="{0} ban expired!" />
  <Translation Id="UnbanFail" Value="{0} doesn't have any active ban." />
  <Translation Id="KickAnnouncement" Value="{0} was kicked by {1} for {2}" />
  <Translation Id="WarnAnnouncement" Value="{0} was warned by {1} for {2}" />
  <Translation Id="CheckownerFail" Value="You are not looking at any barricade, structure or locked vehicle." />
  <Translation Id="CheckownerOwnerName" Value="The owner of this {0} is {1} (banned: {2})" />
  <Translation Id="CheckownerOwnerSteamId" Value="Steam ID: {0}" />
  <Translation Id="CheckownerLastActive" Value="{0} was last active on {1}" />
  <Translation Id="RequireReason" Value="You must specify a reason!" />
  <Translation Id="PlayerInfoMessage" Value="{0} from {1} has {2} bans, {3} kicks &amp; {4} warns" />
  <Translation Id="Permanent" Value="permanent" />
  <Translation Id="PlayerNotFound" Value="Player not found!" />
  <Translation Id="KickInvalid" Value="You must specify target player!" />
  <Translation Id="WarnInvalid" Value="You must specify target player!" />
  <Translation Id="BanInvalid" Value="Ban format: /ban &lt;player&gt; [reason] [duration]" />
  <Translation Id="UnbanNotBanned" Value="{0} doesnt have any active bans!" />
  <Translation Id="UnbanInvalid" Value="You must specify player steamID!" />
  <Translation Id="PlayerInfoInvalid" Value="You must specify target player!" />
  <Translation Id="HWIDBanDisabled" Value="HWID bans are disabled" />
  <Translation Id="IPBanDisabled" Value="IP bans are disabled" />
  <Translation Id="UCSDisabled" Value="UCS service is disabled or API Key is invalid" />
  <Translation Id="ReasonUnspecified" Value="Unspecified" />
  <Translation Id="SpyNoPermission" Value="You don't have permission to spy" />
  <Translation Id="SpyFail" Value="Spy format: /spy &lt;player&gt;" />
  <Translation Id="SpySuccess" Value="Successfully spied {0}!" />
  <Translation Id="SpyCooldown" Value="You have to wait {0} before you can spy again" />
  <Translation Id="MuteAnnouncement" Value="{0} was muted by {1} for {2} for {3}!" />
  <Translation Id="MuteMessage" Value="You cannot use chat, because you are muted for {0} for {1}" />
  <Translation Id="MutesLimit" Value="You can't view the list of all mutes in-game, specify player or use console" />
  <Translation Id="MutesNone" Value="There are no mutes" />
  <Translation Id="MutesLine" Value="MuteID: {0} SteamName: {1} SteamID: {2} PunisherName: {3} PunisherId: {4}" />
  <Translation Id="MuteInvalid" Value="Mute format: /mute &lt;player&gt; [reason] [duration]" />
  <Translation Id="UnmuteInvalid" Value="You must specify player name or steamID!" />
  <Translation Id="UnmuteNotMuted" Value="{0} doesnt have any active mutes!" />
  <Translation Id="UnmuteAnnouncement" Value="{0} was unmuted!" />
  <Translation Id="UnmuteExpired" Value="{0} mute expired!" />
  <Translation Id="MuteBlockCommand" Value="You cannot execute this command, while you are muted for {0} for {1}" />
  <Translation Id="MuteVoice" Value="You cannot use voice chat, because you are muted for {0} for {1}" />
  <Translation Id="UCSBanMessage" Value="[UCS] You are banned. ID: #{0}. Appeal at {1}" />
  <Translation Id="BanAlreadyExistsPermanent" Value="{0} ({1}) is already permanently banned for {2}" />
  <Translation Id="BanAlreadyExists" Value="{0} ({1}) is already banned for {2} for a longer period of {3}" />
  <Translation Id="NoGlitchesFound" Value="Could not find any suspected glitch objects on the map." />
  <Translation Id="GlitchedDestroyed" Value="The glitch ({0}/{1}) was already destroyed. Use command again for next glitch." />
  <Translation Id="GlitchTeleported" Value="Teleported to suspected glitch ({0}/{1}). Use command again for next glitch." />
  <Translation Id="GlitchRescan" Value="Rescanned for glitches. Found {0} suspected objects." />
  <Translation Id="GlitchInvalidIndex" Value="Invalid glitch index {0}. Valid range is 1-{1}." />
</Translations>
```
