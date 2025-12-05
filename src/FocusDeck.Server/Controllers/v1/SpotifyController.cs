using Asp.Versioning;
using FocusDeck.Domain.Entities.Automations;
using FocusDeck.Persistence;
using FocusDeck.Services.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace FocusDeck.Server.Controllers.v1;

[ApiController]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/integrations/spotify")]
[Authorize]
public class SpotifyController : ControllerBase
{
    private readonly AutomationDbContext _db;
    private readonly ISpotifyService _spotifyService;
    private readonly IEncryptionService _encryptionService;

    public SpotifyController(AutomationDbContext db, ISpotifyService spotifyService, IEncryptionService encryptionService)
    {
        _db = db;
        _spotifyService = spotifyService;
        _encryptionService = encryptionService;
    }

    [HttpGet("player")]
    public async Task<ActionResult<SpotifyPlaybackState?>> GetPlayerState(CancellationToken ct)
    {
        var token = await GetAccessTokenAsync(ct);
        if (token == null) return NotFound("Spotify not connected");

        var state = await _spotifyService.GetCurrentlyPlaying(token);
        return Ok(state);
    }

    [HttpPost("play")]
    public async Task<IActionResult> Play(CancellationToken ct)
    {
        var token = await GetAccessTokenAsync(ct);
        if (token == null) return NotFound("Spotify not connected");

        var success = await _spotifyService.Play(token);
        return success ? Ok() : BadRequest("Failed to play");
    }

    [HttpPost("pause")]
    public async Task<IActionResult> Pause(CancellationToken ct)
    {
        var token = await GetAccessTokenAsync(ct);
        if (token == null) return NotFound("Spotify not connected");

        var success = await _spotifyService.Pause(token);
        return success ? Ok() : BadRequest("Failed to pause");
    }

    [HttpGet("playlists")]
    public async Task<ActionResult<List<SpotifyPlaylist>>> GetPlaylists(CancellationToken ct)
    {
        var token = await GetAccessTokenAsync(ct);
        if (token == null) return NotFound("Spotify not connected");

        var playlists = await _spotifyService.GetPlaylists(token);
        return Ok(playlists);
    }

    [HttpPost("playlists/{id}/play")]
    public async Task<IActionResult> PlayPlaylist(string id, CancellationToken ct)
    {
        var token = await GetAccessTokenAsync(ct);
        if (token == null) return NotFound("Spotify not connected");

        var success = await _spotifyService.PlayPlaylist(token, id);
        return success ? Ok() : BadRequest("Failed to play playlist");
    }

    private async Task<string?> GetAccessTokenAsync(CancellationToken ct)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId)) return null;

        var service = await _db.ConnectedServices.AsNoTracking()
            .FirstOrDefaultAsync(s => s.UserId == userId && s.Service == ServiceType.Spotify, ct);

        if (service == null || string.IsNullOrEmpty(service.AccessToken)) return null;

        return _encryptionService.Decrypt(service.AccessToken);
    }
}
