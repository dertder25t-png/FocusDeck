using System.Security.Claims;
using FocusDeck.Domain.Entities;
using FocusDeck.Persistence;
using FocusDeck.Server.Services.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FocusDeck.Server.Controllers.v1;

[ApiController]
[Route("v1/user/settings")]
[Authorize]
public class UserSettingsController : ControllerBase
{
    private readonly AutomationDbContext _db;
    private readonly ILogger<UserSettingsController> _logger;
    private readonly IApiKeyEncryptionService _encryption;

    public UserSettingsController(AutomationDbContext db, ILogger<UserSettingsController> logger, IApiKeyEncryptionService encryption)
    {
        _db = db;
        _logger = logger;
        _encryption = encryption;
    }

    [HttpGet]
    public async Task<ActionResult> Get()
    {
        var (userId, tenantId) = ResolveIdentity();
        if (userId == null || tenantId == null) return Unauthorized();

        var settings = await _db.UserSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.UserId == userId && s.TenantId == tenantId.Value);

        if (settings == null)
        {
            return Ok(new {
                googleApiKey = (string?)null,
                canvasApiToken = (string?)null,
                homeAssistantUrl = (string?)null,
                homeAssistantToken = (string?)null,
                openAiKey = (string?)null,
                anthropicKey = (string?)null,
                updatedAt = (string?)null
            });
        }

        return Ok(new {
            googleApiKey = _encryption.Decrypt(settings.GoogleApiKey ?? string.Empty),
            canvasApiToken = _encryption.Decrypt(settings.CanvasApiToken ?? string.Empty),
            homeAssistantUrl = settings.HomeAssistantUrl,
            homeAssistantToken = _encryption.Decrypt(settings.HomeAssistantToken ?? string.Empty),
            openAiKey = _encryption.Decrypt(settings.OpenAiKey ?? string.Empty),
            anthropicKey = _encryption.Decrypt(settings.AnthropicKey ?? string.Empty),
            updatedAt = settings.UpdatedAt.ToUniversalTime().ToString("O")
        });
    }

    public record UpdateSettingsDto(
        string? GoogleApiKey,
        string? CanvasApiToken,
        string? HomeAssistantUrl,
        string? HomeAssistantToken,
        string? OpenAiKey,
        string? AnthropicKey
    );

    [HttpPost]
    public async Task<ActionResult> Upsert([FromBody] UpdateSettingsDto dto)
    {
        var (userId, tenantId) = ResolveIdentity();
        if (userId == null || tenantId == null) return Unauthorized();

        var settings = await _db.UserSettings.FirstOrDefaultAsync(s => s.UserId == userId && s.TenantId == tenantId.Value);
        var now = DateTime.UtcNow;
        if (settings == null)
        {
            settings = new UserSetting
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId.Value,
                UserId = userId,
                GoogleApiKey = _encryption.Encrypt(dto.GoogleApiKey ?? string.Empty),
                CanvasApiToken = _encryption.Encrypt(dto.CanvasApiToken ?? string.Empty),
                HomeAssistantUrl = dto.HomeAssistantUrl,
                HomeAssistantToken = _encryption.Encrypt(dto.HomeAssistantToken ?? string.Empty),
                OpenAiKey = _encryption.Encrypt(dto.OpenAiKey ?? string.Empty),
                AnthropicKey = _encryption.Encrypt(dto.AnthropicKey ?? string.Empty),
                UpdatedAt = now
            };
            _db.UserSettings.Add(settings);
        }
        else
        {
            settings.GoogleApiKey = _encryption.Encrypt(dto.GoogleApiKey ?? string.Empty);
            settings.CanvasApiToken = _encryption.Encrypt(dto.CanvasApiToken ?? string.Empty);
            settings.HomeAssistantUrl = dto.HomeAssistantUrl;
            settings.HomeAssistantToken = _encryption.Encrypt(dto.HomeAssistantToken ?? string.Empty);
            settings.OpenAiKey = _encryption.Encrypt(dto.OpenAiKey ?? string.Empty);
            settings.AnthropicKey = _encryption.Encrypt(dto.AnthropicKey ?? string.Empty);
            settings.UpdatedAt = now;
        }

        await _db.SaveChangesAsync();

        return Ok(new {
            googleApiKey = _encryption.Decrypt(settings.GoogleApiKey ?? string.Empty),
            canvasApiToken = _encryption.Decrypt(settings.CanvasApiToken ?? string.Empty),
            homeAssistantUrl = settings.HomeAssistantUrl,
            homeAssistantToken = _encryption.Decrypt(settings.HomeAssistantToken ?? string.Empty),
            openAiKey = _encryption.Decrypt(settings.OpenAiKey ?? string.Empty),
            anthropicKey = _encryption.Decrypt(settings.AnthropicKey ?? string.Empty),
            updatedAt = settings.UpdatedAt.ToUniversalTime().ToString("O")
        });
    }

    private (string? userId, Guid? tenantId) ResolveIdentity()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var tenantIdStr = User.FindFirst("app_tenant_id")?.Value;
        if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(tenantIdStr) || !Guid.TryParse(tenantIdStr, out var tenantId))
        {
            return (null, null);
        }
        return (userId, tenantId);
    }
}
