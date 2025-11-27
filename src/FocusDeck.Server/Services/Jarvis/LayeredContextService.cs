using System.Text;
using System.Threading.Tasks;
using FocusDeck.Contracts.DTOs;
using FocusDeck.Contracts.Repositories;
using Microsoft.Extensions.Logging;

namespace FocusDeck.Server.Services.Jarvis
{
    public class LayeredContextService : ILayeredContextService
    {
        private readonly IContextSnapshotRepository _snapshotRepository;
        private readonly ILogger<LayeredContextService> _logger;

        public LayeredContextService(
            IContextSnapshotRepository snapshotRepository,
            ILogger<LayeredContextService> logger)
        {
            _snapshotRepository = snapshotRepository;
            _logger = logger;
        }

        public async Task<LayeredContextDto> BuildContextAsync()
        {
            // TODO: Get the current user ID.
            var userId = Guid.NewGuid();
            _logger.LogInformation("Building layered context for user {UserId}", userId);

            var latestSnapshot = await _snapshotRepository.GetLatestForUserAsync(userId);

            var immediateContext = "No immediate context available.";
            if (latestSnapshot != null)
            {
                var sb = new StringBuilder();
                sb.AppendLine($"User's last known context at {latestSnapshot.Timestamp:O}:");
                foreach (var slice in latestSnapshot.Slices)
                {
                    sb.AppendLine($" - {slice.SourceType}: {slice.Data}");
                }
                immediateContext = sb.ToString();
            }

            // TODO: Implement Session, Project, and Seasonal context retrieval.
            var sessionContext = "Session context not yet implemented.";
            var projectContext = "Project context not yet implemented.";
            var seasonalContext = "Seasonal context not yet implemented.";

            return new LayeredContextDto(
                ImmediateContext: immediateContext,
                SessionContext: sessionContext,
                ProjectContext: projectContext,
                SeasonalContext: seasonalContext
            );
        }
    }
}
