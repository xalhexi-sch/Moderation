using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Moderation.Models;
using Newtonsoft.Json;
using Rocket.Core.Logging;
using SDG.Unturned;

namespace Moderation.Services
{
    public class DiscordWebhookService
    {
        private static readonly HttpClient HttpClient = new HttpClient();
        private readonly DiscordServiceConfiguration config;

        public DiscordWebhookService(DiscordServiceConfiguration config)
        {
            this.config = config;
        }

        public void SendWebhookList(List<WebhookMessage> messages, Dictionary<string, string> placeholders, byte[] imageAttachment = null, string attachmentFilename = "screenshot.jpg")
        {
            if (!config.UseWebhooks || messages == null || messages.Count == 0)
                return;

            foreach (var message in messages)
            {
                if (message.Enabled && !string.IsNullOrWhiteSpace(message.WebhookUrl))
                {
                    Task.Run(async () =>
                    {
                        try
                        {
                            await SendWebhookAsync(message, placeholders, imageAttachment, attachmentFilename);
                        }
                        catch (Exception ex)
                        {
                            Logger.LogWarning($"[Moderation] Error sending Discord webhook: {ex.Message}");
                        }
                    });
                }
            }
        }

        private async Task SendWebhookAsync(WebhookMessage template, Dictionary<string, string> placeholders, byte[] imageAttachment, string attachmentFilename)
        {
            string username = ReplacePlaceholders(template.Username, placeholders);
            string avatarUrl = ReplacePlaceholders(template.AvatarUrl, placeholders);

            var discordPayload = new Dictionary<string, object>
            {
                ["username"] = string.IsNullOrEmpty(username) ? "Moderation" : username
            };

            if (!string.IsNullOrEmpty(avatarUrl))
                discordPayload["avatar_url"] = avatarUrl;

            if (template.Embeds != null && template.Embeds.Count > 0)
            {
                var embedsList = new List<Dictionary<string, object>>();
                foreach (var embed in template.Embeds)
                {
                    var embedObj = new Dictionary<string, object>();

                    if (!string.IsNullOrEmpty(embed.Title))
                        embedObj["title"] = ReplacePlaceholders(embed.Title, placeholders);

                    if (!string.IsNullOrEmpty(embed.Url))
                        embedObj["url"] = ReplacePlaceholders(embed.Url, placeholders);

                    if (!string.IsNullOrEmpty(embed.Description))
                        embedObj["description"] = ReplacePlaceholders(embed.Description, placeholders);

                    if (!string.IsNullOrEmpty(embed.ColorHex))
                    {
                        string hex = embed.ColorHex.Replace("#", "").Trim();
                        if (int.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int colorInt))
                        {
                            embedObj["color"] = colorInt;
                        }
                    }

                    if (embed.WithCurrentTimestamp)
                    {
                        embedObj["timestamp"] = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
                    }

                    if (embed.Author != null && !string.IsNullOrEmpty(embed.Author.Name))
                    {
                        var authorObj = new Dictionary<string, object>
                        {
                            ["name"] = ReplacePlaceholders(embed.Author.Name, placeholders)
                        };
                        if (!string.IsNullOrEmpty(embed.Author.IconUrl))
                            authorObj["icon_url"] = ReplacePlaceholders(embed.Author.IconUrl, placeholders);
                        embedObj["author"] = authorObj;
                    }

                    if (embed.Thumbnail != null && !string.IsNullOrEmpty(embed.Thumbnail.Url))
                    {
                        embedObj["thumbnail"] = new Dictionary<string, object>
                        {
                            ["url"] = ReplacePlaceholders(embed.Thumbnail.Url, placeholders)
                        };
                    }

                    if (imageAttachment != null)
                    {
                        embedObj["image"] = new Dictionary<string, object>
                        {
                            ["url"] = $"attachment://{attachmentFilename}"
                        };
                    }

                    if (embed.Footer != null && !string.IsNullOrEmpty(embed.Footer.Text))
                    {
                        var footerObj = new Dictionary<string, object>
                        {
                            ["text"] = ReplacePlaceholders(embed.Footer.Text, placeholders)
                        };
                        if (!string.IsNullOrEmpty(embed.Footer.IconUrl))
                            footerObj["icon_url"] = ReplacePlaceholders(embed.Footer.IconUrl, placeholders);
                        embedObj["footer"] = footerObj;
                    }

                    if (embed.Fields != null && embed.Fields.Count > 0)
                    {
                        var fieldsList = new List<Dictionary<string, object>>();
                        foreach (var field in embed.Fields)
                        {
                            if (!string.IsNullOrEmpty(field.Name))
                            {
                                fieldsList.Add(new Dictionary<string, object>
                                {
                                    ["name"] = ReplacePlaceholders(field.Name, placeholders),
                                    ["value"] = ReplacePlaceholders(field.Value, placeholders),
                                    ["inline"] = field.Inline
                                });
                            }
                        }
                        embedObj["fields"] = fieldsList;
                    }

                    embedsList.Add(embedObj);
                }
                discordPayload["embeds"] = embedsList;
            }

            string jsonPayload = JsonConvert.SerializeObject(discordPayload);

            if (imageAttachment != null && imageAttachment.Length > 0)
            {
                using (var content = new MultipartFormDataContent())
                {
                    content.Add(new StringContent(jsonPayload, Encoding.UTF8, "application/json"), "payload_json");
                    var imageContent = new ByteArrayContent(imageAttachment);
                    imageContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");
                    content.Add(imageContent, "files[0]", attachmentFilename);

                    var response = await HttpClient.PostAsync(template.WebhookUrl, content);
                    response.EnsureSuccessStatusCode();
                }
            }
            else
            {
                using (var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json"))
                {
                    var response = await HttpClient.PostAsync(template.WebhookUrl, content);
                    response.EnsureSuccessStatusCode();
                }
            }
        }

        public static string ReplacePlaceholders(string text, Dictionary<string, string> placeholders)
        {
            if (string.IsNullOrEmpty(text) || placeholders == null)
                return text ?? string.Empty;

            StringBuilder sb = new StringBuilder(text);
            foreach (var kvp in placeholders)
            {
                sb.Replace("{" + kvp.Key + "}", kvp.Value ?? string.Empty);
            }
            return sb.ToString();
        }
    }
}
