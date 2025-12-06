namespace FocusDeck.Services.Abstractions;

public interface ISpotifyService
{
    Task<SpotifyPlaybackState?> GetCurrentlyPlaying(string accessToken);
    Task<bool> Play(string accessToken);
    Task<bool> Pause(string accessToken);
    Task<bool> SetVolume(string accessToken, int volumePercent);
    Task<List<SpotifyPlaylist>> GetPlaylists(string accessToken);
    Task<bool> PlayPlaylist(string accessToken, string playlistId);
}

public class SpotifyPlaybackState
{
    public string Track { get; set; } = "Unknown Track";
    public string Artist { get; set; } = "Unknown Artist";
    public string Album { get; set; } = "Unknown Album";
    public bool IsPlaying { get; set; }
    public long ProgressMs { get; set; }
    public long DurationMs { get; set; }
    public string Uri { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
}

public class SpotifyPlaylist
{
    public string Id { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? ImageUrl { get; set; }
    public string? Uri { get; set; }
}
