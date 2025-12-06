using System.Text.Json;
using FocusDeck.Services.Abstractions;

namespace FocusDeck.Server.Services.Integrations
{
    /// <summary>
    /// Service for integrating with Spotify API
    /// </summary>
    public class SpotifyService : ISpotifyService
    {
        private readonly ILogger<SpotifyService> _logger;
        private readonly HttpClient _httpClient;

        public SpotifyService(ILogger<SpotifyService> logger, HttpClient? httpClient = null)
        {
            _logger = logger;
            _httpClient = httpClient ?? new HttpClient();
        }

        public async Task<SpotifyPlaybackState?> GetCurrentlyPlaying(string accessToken)
        {
            try
            {
                var url = "https://api.spotify.com/v1/me/player/currently-playing";

                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");

                var response = await _httpClient.GetAsync(url);
                if (response.StatusCode == System.Net.HttpStatusCode.NoContent)
                {
                    return null; // Nothing playing
                }

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var doc = JsonDocument.Parse(json).RootElement;

                    var isPlaying = doc.TryGetProperty("is_playing", out var ip) && ip.GetBoolean();
                    var progressMs = doc.TryGetProperty("progress_ms", out var pm) ? pm.GetInt64() : 0;

                    var item = doc.TryGetProperty("item", out var i) && i.ValueKind != JsonValueKind.Null ? i : default;
                    if (item.ValueKind == JsonValueKind.Undefined) return null;

                    var trackName = item.TryGetProperty("name", out var n) ? n.GetString() ?? "Unknown" : "Unknown";
                    var durationMs = item.TryGetProperty("duration_ms", out var dm) ? dm.GetInt64() : 0;
                    var uri = item.TryGetProperty("uri", out var u) ? u.GetString() ?? "" : "";

                    var artistName = "Unknown Artist";
                    if (item.TryGetProperty("artists", out var artists) && artists.GetArrayLength() > 0)
                    {
                        artistName = artists[0].TryGetProperty("name", out var an) ? an.GetString() ?? "Unknown" : "Unknown";
                    }

                    var albumName = "Unknown Album";
                    string? imageUrl = null;
                    if (item.TryGetProperty("album", out var album))
                    {
                        albumName = album.TryGetProperty("name", out var aln) ? aln.GetString() ?? "Unknown" : "Unknown";
                        if (album.TryGetProperty("images", out var images) && images.GetArrayLength() > 0)
                        {
                            imageUrl = images[0].TryGetProperty("url", out var img) ? img.GetString() : null;
                        }
                    }

                    return new SpotifyPlaybackState
                    {
                        Track = trackName,
                        Artist = artistName,
                        Album = albumName,
                        IsPlaying = isPlaying,
                        ProgressMs = progressMs,
                        DurationMs = durationMs,
                        Uri = uri,
                        ImageUrl = imageUrl
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting currently playing Spotify track");
            }
            return null;
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
                    JsonSerializer.Serialize(body),
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

        public async Task<List<SpotifyPlaylist>> GetPlaylists(string accessToken)
        {
            try
            {
                var url = "https://api.spotify.com/v1/me/playlists?limit=20";

                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");

                var response = await _httpClient.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var doc = JsonDocument.Parse(json).RootElement;

                    var list = new List<SpotifyPlaylist>();
                    if (doc.TryGetProperty("items", out var items))
                    {
                        foreach (var item in items.EnumerateArray())
                        {
                            var id = item.TryGetProperty("id", out var i) ? i.GetString() : null;
                            var name = item.TryGetProperty("name", out var n) ? n.GetString() : "Untitled";
                            var uri = item.TryGetProperty("uri", out var u) ? u.GetString() : null;
                            string? imgUrl = null;

                            if (item.TryGetProperty("images", out var images) && images.ValueKind == JsonValueKind.Array && images.GetArrayLength() > 0)
                            {
                                imgUrl = images[0].TryGetProperty("url", out var iu) ? iu.GetString() : null;
                            }

                            if (id != null)
                            {
                                list.Add(new SpotifyPlaylist
                                {
                                    Id = id,
                                    Name = name ?? "Untitled",
                                    Uri = uri,
                                    ImageUrl = imgUrl
                                });
                            }
                        }
                    }
                    return list;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting Spotify playlists");
            }
            return new List<SpotifyPlaylist>();
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
