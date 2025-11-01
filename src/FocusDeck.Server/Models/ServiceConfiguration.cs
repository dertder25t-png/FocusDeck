using System;
using System.ComponentModel.DataAnnotations;

namespace FocusDeck.Server.Models
{
    /// <summary>
    /// Stores OAuth client credentials and other service configuration
    /// that users can configure through the UI without editing appsettings.json
    /// </summary>
    public class ServiceConfiguration
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string ServiceName { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? ClientId { get; set; }

        [MaxLength(500)]
        public string? ClientSecret { get; set; }

        [MaxLength(500)]
        public string? ApiKey { get; set; }

        /// <summary>
        /// Additional configuration stored as JSON
        /// </summary>
        public string? AdditionalConfig { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
