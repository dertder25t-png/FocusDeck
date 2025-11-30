using FocusDeck.SharedKernel.Tenancy;

namespace FocusDeck.Domain.Entities;

public class UserSetting : IMustHaveTenant
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string UserId { get; set; } = string.Empty;

    public string? GoogleApiKey { get; set; }
    public string? CanvasApiToken { get; set; }
    public string? HomeAssistantUrl { get; set; }
    public string? HomeAssistantToken { get; set; }
    public string? OpenAiKey { get; set; }
    public string? AnthropicKey { get; set; }

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
