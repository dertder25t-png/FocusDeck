using System.Collections.Generic;

namespace FocusDeck.Server.Controllers.Models
{
    public sealed class UpdateResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public bool IsUpdating { get; set; }
    }

    public sealed class UpdateStatusResponse
    {
        public bool IsUpdating { get; set; }
        public bool IsLinux { get; set; }
        public string RepositoryPath { get; set; } = string.Empty;
        public string ConfigurationStatus { get; set; } = string.Empty;
        public string? LastUpdateLog { get; set; }
    }

    public sealed class ConfigCheckResponse
    {
        public bool IsConfigured { get; set; }
        public string Message { get; set; } = string.Empty;
        public string Platform { get; set; } = string.Empty;
        public string? RepositoryPath { get; set; }
        public List<ConfigCheck> Checks { get; set; } = new();
    }

    public sealed class ConfigCheck
    {
        public string Name { get; set; } = string.Empty;
        public bool Passed { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public sealed class UpdateAvailabilityResult
    {
        public bool UpdateAvailable { get; set; }
        public string Message { get; set; } = string.Empty;
        public string CurrentCommit { get; set; } = "unknown";
        public string CurrentDate { get; set; } = string.Empty;
        public string LatestCommit { get; set; } = string.Empty;
        public string LatestDate { get; set; } = string.Empty;
        public string LatestMessage { get; set; } = string.Empty;
    }
}
