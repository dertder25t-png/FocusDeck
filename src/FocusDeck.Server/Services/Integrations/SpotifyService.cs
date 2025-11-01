namespace FocusDeck.Server.Services.Integrations
{
    /// <summary>
    /// Service for integrating with Spotify API
    /// </summary>
    public class SpotifyService
    {
        private readonly ILogger<SpotifyService> _logger;
        private readonly HttpClient _httpClient;

        public SpotifyService(ILogger<SpotifyService> logger)
        {
            _logger = logger;
            _httpClient = new HttpClient();
        }

        public async Task<bool> Play(string accessToken)
        {
            return await ControlPlayback(accessToken, "https://api.spotify.com/v1/me/player/play", "PUT");
        }

        public async Task<bool> Pause(string accessToken)
        {
            return await ControlPlayback(accessToken, "https://api.spotify.com/v1/me/player/pause", "PUT");
        }

        public async Task<bool> PlayPlaylist(string accessToken, string playlistId)
        {
            try
            {
                var url = "https://api.spotify.com/v1/me/player/play";
                
                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");

                var body = new { context_uri = $"spotify:playlist:{playlistId}" };
                var content = new StringContent(
                    System.Text.Json.JsonSerializer.Serialize(body),
                    System.Text.Encoding.UTF8,
                    "application/json"
                );

                var response = await _httpClient.PutAsync(url, content);
                
                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation($"Started playing playlist {playlistId}");
                    return true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error playing Spotify playlist");
            }

            return false;
        }

        public async Task<bool> SetVolume(string accessToken, int volumePercent)
        {
            try
            {
                var url = $"https://api.spotify.com/v1/me/player/volume?volume_percent={volumePercent}";
                
                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");

                var response = await _httpClient.PutAsync(url, null);
                
                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation($"Set Spotify volume to {volumePercent}%");
                    return true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting Spotify volume");
            }

            return false;
        }

        private async Task<bool> ControlPlayback(string accessToken, string url, string method)
        {
            try
            {
                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");

                var request = new HttpRequestMessage(new HttpMethod(method), url);
                var response = await _httpClient.SendAsync(request);
                
                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation($"Spotify {method} successful");
                    return true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error controlling Spotify playback: {method}");
            }

            return false;
        }
    }
}
