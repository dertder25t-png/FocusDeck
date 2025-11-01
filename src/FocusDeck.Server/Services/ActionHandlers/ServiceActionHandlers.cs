using FocusDeck.Shared.Models.Automations;
using FocusDeck.Server.Data;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Net.Http.Headers;

namespace FocusDeck.Server.Services.ActionHandlers
{
    /// <summary>
    /// Base interface for action handlers
    /// </summary>
    public interface IActionHandler
    {
        string ServiceName { get; }
        Task<ActionResult> ExecuteAsync(AutomationAction action, AutomationDbContext db, ILogger logger);
    }

    public class ActionResult
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public object? Data { get; set; }
    }

    /// <summary>
    /// Spotify action handler
    /// </summary>
    public class SpotifyActionHandler : IActionHandler
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public string ServiceName => "Spotify";

        public SpotifyActionHandler(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public async Task<ActionResult> ExecuteAsync(AutomationAction action, AutomationDbContext db, ILogger logger)
        {
            var service = await GetConnectedService(db);
            if (service == null)
                return new ActionResult { Success = false, Message = "Spotify not connected" };

            var httpClient = _httpClientFactory.CreateClient();
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", service.AccessToken);

            try
            {
                return action.ActionType switch
                {
                    ActionTypes.SpotifyPlay => await PlaySpotify(httpClient, action),
                    ActionTypes.SpotifyPause => await PauseSpotify(httpClient),
                    ActionTypes.SpotifyNext => await NextTrack(httpClient),
                    ActionTypes.SpotifyPrevious => await PreviousTrack(httpClient),
                    ActionTypes.SpotifySetVolume => await SetVolume(httpClient, action),
                    ActionTypes.SpotifyPlayPlaylist => await PlayPlaylist(httpClient, action),
                    _ => new ActionResult { Success = false, Message = $"Unknown action: {action.ActionType}" }
                };
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Spotify action failed: {ActionType}", action.ActionType);
                return new ActionResult { Success = false, Message = ex.Message };
            }
        }

        private async Task<ConnectedService?> GetConnectedService(AutomationDbContext db)
        {
            return await db.ConnectedServices
                .FirstOrDefaultAsync(s => s.Service == ServiceType.Spotify);
        }

        private async Task<ActionResult> PlaySpotify(HttpClient client, AutomationAction action)
        {
            var response = await client.PutAsync("https://api.spotify.com/v1/me/player/play", null);
            return new ActionResult { Success = response.IsSuccessStatusCode, Message = "Playback started" };
        }

        private async Task<ActionResult> PauseSpotify(HttpClient client)
        {
            var response = await client.PutAsync("https://api.spotify.com/v1/me/player/pause", null);
            return new ActionResult { Success = response.IsSuccessStatusCode, Message = "Playback paused" };
        }

        private async Task<ActionResult> NextTrack(HttpClient client)
        {
            var response = await client.PostAsync("https://api.spotify.com/v1/me/player/next", null);
            return new ActionResult { Success = response.IsSuccessStatusCode, Message = "Skipped to next track" };
        }

        private async Task<ActionResult> PreviousTrack(HttpClient client)
        {
            var response = await client.PostAsync("https://api.spotify.com/v1/me/player/previous", null);
            return new ActionResult { Success = response.IsSuccessStatusCode, Message = "Skipped to previous track" };
        }

        private async Task<ActionResult> SetVolume(HttpClient client, AutomationAction action)
        {
            var volume = action.Settings.GetValueOrDefault("volume", "50");
            var response = await client.PutAsync($"https://api.spotify.com/v1/me/player/volume?volume_percent={volume}", null);
            return new ActionResult { Success = response.IsSuccessStatusCode, Message = $"Volume set to {volume}%" };
        }

        private async Task<ActionResult> PlayPlaylist(HttpClient client, AutomationAction action)
        {
            var playlistUri = action.Settings.GetValueOrDefault("playlistUri", "");
            if (string.IsNullOrEmpty(playlistUri))
                return new ActionResult { Success = false, Message = "Playlist URI required" };

            var payload = JsonSerializer.Serialize(new { context_uri = playlistUri });
            var content = new StringContent(payload, System.Text.Encoding.UTF8, "application/json");
            var response = await client.PutAsync("https://api.spotify.com/v1/me/player/play", content);
            return new ActionResult { Success = response.IsSuccessStatusCode, Message = "Playing playlist" };
        }
    }

    /// <summary>
    /// Home Assistant action handler
    /// </summary>
    public class HomeAssistantActionHandler : IActionHandler
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public string ServiceName => "HomeAssistant";

        public HomeAssistantActionHandler(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public async Task<ActionResult> ExecuteAsync(AutomationAction action, AutomationDbContext db, ILogger logger)
        {
            var service = await db.ConnectedServices
                .FirstOrDefaultAsync(s => s.Service == Shared.Models.Automations.ServiceType.HomeAssistant);

            if (service == null)
                return new ActionResult { Success = false, Message = "Home Assistant not connected" };

            var metadata = JsonSerializer.Deserialize<Dictionary<string, string>>(service.MetadataJson ?? "{}");
            var baseUrl = metadata?.GetValueOrDefault("haBaseUrl", "");

            if (string.IsNullOrEmpty(baseUrl))
                return new ActionResult { Success = false, Message = "Home Assistant URL not configured" };

            var httpClient = _httpClientFactory.CreateClient();
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", service.AccessToken);

            try
            {
                return action.ActionType switch
                {
                    ActionTypes.HomeAssistantTurnOn => await CallService(httpClient, baseUrl, "light", "turn_on", action),
                    ActionTypes.HomeAssistantTurnOff => await CallService(httpClient, baseUrl, "light", "turn_off", action),
                    ActionTypes.HomeAssistantSetBrightness => await SetBrightness(httpClient, baseUrl, action),
                    ActionTypes.HomeAssistantSetColor => await SetColor(httpClient, baseUrl, action),
                    ActionTypes.HomeAssistantActivateScene => await TriggerScene(httpClient, baseUrl, action),
                    ActionTypes.HomeAssistantCallService => await CallCustomService(httpClient, baseUrl, action),
                    _ => new ActionResult { Success = false, Message = $"Unknown action: {action.ActionType}" }
                };
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Home Assistant action failed: {ActionType}", action.ActionType);
                return new ActionResult { Success = false, Message = ex.Message };
            }
        }

        private async Task<ActionResult> CallService(HttpClient client, string baseUrl, string domain, string service, AutomationAction action)
        {
            var entityId = action.Settings.GetValueOrDefault("entityId", "");
            if (string.IsNullOrEmpty(entityId))
                return new ActionResult { Success = false, Message = "Entity ID required" };

            var payload = JsonSerializer.Serialize(new { entity_id = entityId });
            var content = new StringContent(payload, System.Text.Encoding.UTF8, "application/json");
            var response = await client.PostAsync($"{baseUrl}/api/services/{domain}/{service}", content);

            return new ActionResult { Success = response.IsSuccessStatusCode, Message = $"Called {domain}.{service}" };
        }

        private async Task<ActionResult> SetBrightness(HttpClient client, string baseUrl, AutomationAction action)
        {
            var entityId = action.Settings.GetValueOrDefault("entityId", "");
            var brightness = action.Settings.GetValueOrDefault("brightness", "255");

            var payload = JsonSerializer.Serialize(new { entity_id = entityId, brightness = int.Parse(brightness) });
            var content = new StringContent(payload, System.Text.Encoding.UTF8, "application/json");
            var response = await client.PostAsync($"{baseUrl}/api/services/light/turn_on", content);

            return new ActionResult { Success = response.IsSuccessStatusCode, Message = $"Brightness set to {brightness}" };
        }

        private async Task<ActionResult> SetColor(HttpClient client, string baseUrl, AutomationAction action)
        {
            var entityId = action.Settings.GetValueOrDefault("entityId", "");
            var rgb = action.Settings.GetValueOrDefault("rgb", "255,255,255").Split(',').Select(int.Parse).ToArray();

            var payload = JsonSerializer.Serialize(new { entity_id = entityId, rgb_color = rgb });
            var content = new StringContent(payload, System.Text.Encoding.UTF8, "application/json");
            var response = await client.PostAsync($"{baseUrl}/api/services/light/turn_on", content);

            return new ActionResult { Success = response.IsSuccessStatusCode, Message = "Color set" };
        }

        private async Task<ActionResult> TriggerScene(HttpClient client, string baseUrl, AutomationAction action)
        {
            var sceneId = action.Settings.GetValueOrDefault("sceneId", "");
            var payload = JsonSerializer.Serialize(new { entity_id = sceneId });
            var content = new StringContent(payload, System.Text.Encoding.UTF8, "application/json");
            var response = await client.PostAsync($"{baseUrl}/api/services/scene/turn_on", content);

            return new ActionResult { Success = response.IsSuccessStatusCode, Message = "Scene triggered" };
        }

        private async Task<ActionResult> CallCustomService(HttpClient client, string baseUrl, AutomationAction action)
        {
            var domain = action.Settings.GetValueOrDefault("domain", "");
            var service = action.Settings.GetValueOrDefault("service", "");
            var data = action.Settings.GetValueOrDefault("data", "{}");

            var content = new StringContent(data, System.Text.Encoding.UTF8, "application/json");
            var response = await client.PostAsync($"{baseUrl}/api/services/{domain}/{service}", content);

            return new ActionResult { Success = response.IsSuccessStatusCode, Message = $"Called {domain}.{service}" };
        }
    }

    /// <summary>
    /// Philips Hue action handler
    /// </summary>
    public class PhilipsHueActionHandler : IActionHandler
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public string ServiceName => "PhilipsHue";

        public PhilipsHueActionHandler(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public async Task<ActionResult> ExecuteAsync(AutomationAction action, AutomationDbContext db, ILogger logger)
        {
            var service = await db.ConnectedServices
                .FirstOrDefaultAsync(s => s.Service == Shared.Models.Automations.ServiceType.PhilipsHue);

            if (service == null)
                return new ActionResult { Success = false, Message = "Philips Hue not connected" };

            var metadata = JsonSerializer.Deserialize<Dictionary<string, string>>(service.MetadataJson ?? "{}");
            var bridgeIp = metadata?.GetValueOrDefault("bridgeIp", "");
            var username = service.AccessToken;

            if (string.IsNullOrEmpty(bridgeIp))
                return new ActionResult { Success = false, Message = "Bridge IP not configured" };

            var httpClient = _httpClientFactory.CreateClient();
            var baseUrl = $"http://{bridgeIp}/api/{username}";

            try
            {
                return action.ActionType switch
                {
                    ActionTypes.HueTurnOn => await SetLightState(httpClient, baseUrl, action, true),
                    ActionTypes.HueTurnOff => await SetLightState(httpClient, baseUrl, action, false),
                    ActionTypes.HueSetBrightness => await SetBrightness(httpClient, baseUrl, action),
                    ActionTypes.HueSetColor => await SetColor(httpClient, baseUrl, action),
                    ActionTypes.HueFlash => await FlashLights(httpClient, baseUrl, action),
                    _ => new ActionResult { Success = false, Message = $"Unknown action: {action.ActionType}" }
                };
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Philips Hue action failed: {ActionType}", action.ActionType);
                return new ActionResult { Success = false, Message = ex.Message };
            }
        }

        private async Task<ActionResult> SetLightState(HttpClient client, string baseUrl, AutomationAction action, bool on)
        {
            var lightId = action.Settings.GetValueOrDefault("lightId", "1");
            var payload = JsonSerializer.Serialize(new { on });
            var content = new StringContent(payload, System.Text.Encoding.UTF8, "application/json");
            var response = await client.PutAsync($"{baseUrl}/lights/{lightId}/state", content);

            return new ActionResult { Success = response.IsSuccessStatusCode, Message = $"Light {(on ? "on" : "off")}" };
        }

        private async Task<ActionResult> SetBrightness(HttpClient client, string baseUrl, AutomationAction action)
        {
            var lightId = action.Settings.GetValueOrDefault("lightId", "1");
            var brightness = action.Settings.GetValueOrDefault("brightness", "254");

            var payload = JsonSerializer.Serialize(new { on = true, bri = int.Parse(brightness) });
            var content = new StringContent(payload, System.Text.Encoding.UTF8, "application/json");
            var response = await client.PutAsync($"{baseUrl}/lights/{lightId}/state", content);

            return new ActionResult { Success = response.IsSuccessStatusCode, Message = $"Brightness set to {brightness}" };
        }

        private async Task<ActionResult> SetColor(HttpClient client, string baseUrl, AutomationAction action)
        {
            var lightId = action.Settings.GetValueOrDefault("lightId", "1");
            var hue = action.Settings.GetValueOrDefault("hue", "0");
            var sat = action.Settings.GetValueOrDefault("saturation", "254");

            var payload = JsonSerializer.Serialize(new { on = true, hue = int.Parse(hue), sat = int.Parse(sat) });
            var content = new StringContent(payload, System.Text.Encoding.UTF8, "application/json");
            var response = await client.PutAsync($"{baseUrl}/lights/{lightId}/state", content);

            return new ActionResult { Success = response.IsSuccessStatusCode, Message = "Color set" };
        }

        private async Task<ActionResult> FlashLights(HttpClient client, string baseUrl, AutomationAction action)
        {
            var lightId = action.Settings.GetValueOrDefault("lightId", "1");
            var payload = JsonSerializer.Serialize(new { alert = "select" });
            var content = new StringContent(payload, System.Text.Encoding.UTF8, "application/json");
            var response = await client.PutAsync($"{baseUrl}/lights/{lightId}/state", content);

            return new ActionResult { Success = response.IsSuccessStatusCode, Message = "Lights flashed" };
        }
    }

    /// <summary>
    /// Slack action handler
    /// </summary>
    public class SlackActionHandler : IActionHandler
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public string ServiceName => "Slack";

        public SlackActionHandler(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public async Task<ActionResult> ExecuteAsync(AutomationAction action, AutomationDbContext db, ILogger logger)
        {
            var service = await db.ConnectedServices
                .FirstOrDefaultAsync(s => s.Service == Shared.Models.Automations.ServiceType.Slack);

            if (service == null)
                return new ActionResult { Success = false, Message = "Slack not connected" };

            var httpClient = _httpClientFactory.CreateClient();
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", service.AccessToken);

            try
            {
                return action.ActionType switch
                {
                    ActionTypes.SlackSendMessage => await SendMessage(httpClient, action),
                    ActionTypes.SlackUpdateStatus => await UpdateStatus(httpClient, action),
                    ActionTypes.SlackSetCustomStatus => await SetCustomStatus(httpClient, action),
                    _ => new ActionResult { Success = false, Message = $"Unknown action: {action.ActionType}" }
                };
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Slack action failed: {ActionType}", action.ActionType);
                return new ActionResult { Success = false, Message = ex.Message };
            }
        }

        private async Task<ActionResult> SendMessage(HttpClient client, AutomationAction action)
        {
            var channel = action.Settings.GetValueOrDefault("channel", "");
            var text = action.Settings.GetValueOrDefault("text", "");

            var payload = JsonSerializer.Serialize(new { channel, text });
            var content = new StringContent(payload, System.Text.Encoding.UTF8, "application/json");
            var response = await client.PostAsync("https://slack.com/api/chat.postMessage", content);

            return new ActionResult { Success = response.IsSuccessStatusCode, Message = "Message sent" };
        }

        private async Task<ActionResult> UpdateStatus(HttpClient client, AutomationAction action)
        {
            var statusText = action.Settings.GetValueOrDefault("statusText", "");
            var statusEmoji = action.Settings.GetValueOrDefault("statusEmoji", ":speech_balloon:");

            var profile = new { status_text = statusText, status_emoji = statusEmoji };
            var payload = JsonSerializer.Serialize(new { profile });
            var content = new StringContent(payload, System.Text.Encoding.UTF8, "application/json");
            var response = await client.PostAsync("https://slack.com/api/users.profile.set", content);

            return new ActionResult { Success = response.IsSuccessStatusCode, Message = "Status updated" };
        }

        private async Task<ActionResult> SetCustomStatus(HttpClient client, AutomationAction action)
        {
            return await UpdateStatus(client, action);
        }
    }

    /// <summary>
    /// Discord webhook action handler
    /// </summary>
    public class DiscordActionHandler : IActionHandler
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public string ServiceName => "Discord";

        public DiscordActionHandler(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public async Task<ActionResult> ExecuteAsync(AutomationAction action, AutomationDbContext db, ILogger logger)
        {
            var service = await db.ConnectedServices
                .FirstOrDefaultAsync(s => s.Service == Shared.Models.Automations.ServiceType.Discord);

            if (service == null)
                return new ActionResult { Success = false, Message = "Discord not connected" };

            var metadata = JsonSerializer.Deserialize<Dictionary<string, string>>(service.MetadataJson ?? "{}");
            var webhookUrl = metadata?.GetValueOrDefault("webhookUrl", "");

            if (string.IsNullOrEmpty(webhookUrl))
                return new ActionResult { Success = false, Message = "Webhook URL not configured" };

            var httpClient = _httpClientFactory.CreateClient();

            try
            {
                return action.ActionType switch
                {
                    ActionTypes.DiscordSendMessage => await SendMessage(httpClient, webhookUrl, action),
                    ActionTypes.DiscordSendEmbed => await SendEmbed(httpClient, webhookUrl, action),
                    _ => new ActionResult { Success = false, Message = $"Unknown action: {action.ActionType}" }
                };
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Discord action failed: {ActionType}", action.ActionType);
                return new ActionResult { Success = false, Message = ex.Message };
            }
        }

        private async Task<ActionResult> SendMessage(HttpClient client, string webhookUrl, AutomationAction action)
        {
            var content = action.Settings.GetValueOrDefault("content", "");
            var payload = JsonSerializer.Serialize(new { content });
            var httpContent = new StringContent(payload, System.Text.Encoding.UTF8, "application/json");
            var response = await client.PostAsync(webhookUrl, httpContent);

            return new ActionResult { Success = response.IsSuccessStatusCode, Message = "Message sent to Discord" };
        }

        private async Task<ActionResult> SendEmbed(HttpClient client, string webhookUrl, AutomationAction action)
        {
            var title = action.Settings.GetValueOrDefault("title", "");
            var description = action.Settings.GetValueOrDefault("description", "");
            var color = int.Parse(action.Settings.GetValueOrDefault("color", "3447003"));

            var embed = new { title, description, color };
            var payload = JsonSerializer.Serialize(new { embeds = new[] { embed } });
            var httpContent = new StringContent(payload, System.Text.Encoding.UTF8, "application/json");
            var response = await client.PostAsync(webhookUrl, httpContent);

            return new ActionResult { Success = response.IsSuccessStatusCode, Message = "Embed sent to Discord" };
        }
    }
}
