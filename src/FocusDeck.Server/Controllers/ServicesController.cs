using Microsoft.AspNetCore.Mvc;
using FocusDeck.Shared.Models.Automations;

namespace FocusDeck.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ServicesController : ControllerBase
    {
        private static readonly List<ConnectedService> _services = new();

        [HttpGet]
        public ActionResult<List<ConnectedService>> GetAll()
        {
            // Don't return sensitive tokens to client
            var sanitized = _services.Select(s => new ConnectedService
            {
                Id = s.Id,
                UserId = s.UserId,
                Service = s.Service,
                AccessToken = "***",
                RefreshToken = "***",
                ExpiresAt = s.ExpiresAt,
                ConnectedAt = s.ConnectedAt
            }).ToList();

            return Ok(sanitized);
        }

        [HttpPost("connect/{service}")]
        public ActionResult<ConnectedService> Connect(ServiceType service, [FromBody] Dictionary<string, string> credentials)
        {
            // This would typically involve OAuth flow
            var connectedService = new ConnectedService
            {
                Id = Guid.NewGuid(),
                UserId = "default_user", // Replace with actual user system
                Service = service,
                AccessToken = credentials.GetValueOrDefault("access_token", ""),
                RefreshToken = credentials.GetValueOrDefault("refresh_token", ""),
                ExpiresAt = DateTime.UtcNow.AddDays(60),
                ConnectedAt = DateTime.UtcNow
            };

            _services.Add(connectedService);

            return Ok(new { message = $"{service} connected successfully", serviceId = connectedService.Id });
        }

        [HttpDelete("{id}")]
        public ActionResult Disconnect(Guid id)
        {
            var service = _services.FirstOrDefault(s => s.Id == id);
            if (service == null)
                return NotFound();

            _services.Remove(service);
            return NoContent();
        }

        [HttpGet("oauth/{service}/url")]
        public ActionResult<string> GetOAuthUrl(ServiceType service)
        {
            // Generate OAuth URLs for different services
            var urls = new Dictionary<ServiceType, string>
            {
                { ServiceType.GoogleCalendar, "https://accounts.google.com/o/oauth2/v2/auth?client_id=YOUR_CLIENT_ID&redirect_uri=YOUR_REDIRECT&scope=https://www.googleapis.com/auth/calendar.readonly" },
                { ServiceType.Spotify, "https://accounts.spotify.com/authorize?client_id=YOUR_CLIENT_ID&response_type=code&redirect_uri=YOUR_REDIRECT&scope=user-modify-playback-state%20user-read-playback-state" },
                { ServiceType.Canvas, "/api/services/canvas/setup" },
                { ServiceType.HomeAssistant, "/api/services/homeassistant/setup" }
            };

            if (urls.TryGetValue(service, out var url))
                return Ok(new { url });

            return BadRequest(new { message = "OAuth not supported for this service" });
        }
    }
}
