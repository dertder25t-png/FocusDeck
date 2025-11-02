namespace FocusDeck.Server.Controllers.Models
{
    /// <summary>
    /// DTO for saving service configuration from the UI.
    /// </summary>
    public sealed class ServiceConfigDto
    {
        public string? ClientId { get; set; }
        public string? ClientSecret { get; set; }
        public string? ApiKey { get; set; }
        public string? AdditionalConfig { get; set; }
    }
}
