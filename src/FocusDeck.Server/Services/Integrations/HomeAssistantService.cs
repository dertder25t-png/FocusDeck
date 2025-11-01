namespace FocusDeck.Server.Services.Integrations
{
    /// <summary>
    /// Service for integrating with Home Assistant
    /// </summary>
    public class HomeAssistantService
    {
        private readonly ILogger<HomeAssistantService> _logger;
        private readonly HttpClient _httpClient;

        public HomeAssistantService(ILogger<HomeAssistantService> logger)
        {
            _logger = logger;
            _httpClient = new HttpClient();
        }

        public async Task<bool> TurnOn(string homeAssistantUrl, string accessToken, string entityId)
        {
            return await CallService(homeAssistantUrl, accessToken, "homeassistant", "turn_on", new { entity_id = entityId });
        }

        public async Task<bool> TurnOff(string homeAssistantUrl, string accessToken, string entityId)
        {
            return await CallService(homeAssistantUrl, accessToken, "homeassistant", "turn_off", new { entity_id = entityId });
        }

        public async Task<bool> SetLightScene(string homeAssistantUrl, string accessToken, string sceneId)
        {
            return await CallService(homeAssistantUrl, accessToken, "scene", "turn_on", new { entity_id = sceneId });
        }

        public async Task<bool> SetLightBrightness(string homeAssistantUrl, string accessToken, string entityId, int brightness)
        {
            return await CallService(homeAssistantUrl, accessToken, "light", "turn_on", new 
            { 
                entity_id = entityId,
                brightness = brightness
            });
        }

        public async Task<bool> CallService(string homeAssistantUrl, string accessToken, string domain, string service, object data)
        {
            try
            {
                var url = $"{homeAssistantUrl}/api/services/{domain}/{service}";
                
                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");

                var content = new StringContent(
                    System.Text.Json.JsonSerializer.Serialize(data),
                    System.Text.Encoding.UTF8,
                    "application/json"
                );

                var response = await _httpClient.PostAsync(url, content);
                
                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation($"Home Assistant service call successful: {domain}.{service}");
                    return true;
                }
                else
                {
                    _logger.LogWarning($"Home Assistant service call failed: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error calling Home Assistant service: {domain}.{service}");
            }

            return false;
        }

        public async Task<EntityState?> GetEntityState(string homeAssistantUrl, string accessToken, string entityId)
        {
            try
            {
                var url = $"{homeAssistantUrl}/api/states/{entityId}";
                
                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");

                var response = await _httpClient.GetAsync(url);
                
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    _logger.LogInformation($"Successfully fetched state for {entityId}");
                    return System.Text.Json.JsonSerializer.Deserialize<EntityState>(json);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting Home Assistant entity state: {entityId}");
            }

            return null;
        }
    }

    public class EntityState
    {
        public string entity_id { get; set; } = null!;
        public string state { get; set; } = null!;
        public Dictionary<string, object>? attributes { get; set; }
        public DateTime last_changed { get; set; }
        public DateTime last_updated { get; set; }
    }
}
