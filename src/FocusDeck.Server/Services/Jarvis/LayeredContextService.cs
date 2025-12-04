using System.Text;
using System.Threading.Tasks;
using FocusDeck.Contracts.DTOs;
using FocusDeck.Contracts.Repositories;
using FocusDeck.Contracts.Services.Context;
using FocusDeck.SharedKernel.Tenancy;
using Microsoft.Extensions.Logging;

namespace FocusDeck.Server.Services.Jarvis
{
    public class LayeredContextService : ILayeredContextService
    {
        private readonly IContextSnapshotRepository _snapshotRepository;
        private readonly IContextRetrievalService _retrievalService;
        private readonly ICurrentTenant _currentTenant;
        private readonly ILogger<LayeredContextService> _logger;

        public LayeredContextService(
            IContextSnapshotRepository snapshotRepository,
            IContextRetrievalService retrievalService,
            ICurrentTenant currentTenant,
            ILogger<LayeredContextService> logger)
        {
            _snapshotRepository = snapshotRepository;
            _retrievalService = retrievalService;
            _currentTenant = currentTenant;
            _logger = logger;
        }

        public async Task<LayeredContextDto> BuildContextAsync(Guid userId)
        {
            _logger.LogInformation("Building layered context for user {UserId}", userId);

            var latestSnapshot = await _snapshotRepository.GetLatestForUserAsync(userId);

            var immediateContext = "No immediate context available.";
            var sessionContext = "No session context available.";
            var projectContext = "No project context available."; // Placeholder for future project inference
            var seasonalContext = "No seasonal context available."; // Placeholder for future seasonal data

            if (latestSnapshot != null)
            {
                // 1. Build Immediate Context (Current Window/Activity)
                var sb = new StringBuilder();
                sb.AppendLine($"User's last known context at {latestSnapshot.Timestamp:O}:");
                foreach (var slice in latestSnapshot.Slices)
                {
                    sb.AppendLine($" - {slice.SourceType}: {slice.Data}");
                }
                immediateContext = sb.ToString();

                // 2. Build Session Context (Retrieve Similar Historical Moments)
                var similarMoments = await _retrievalService.GetSimilarMomentsAsync(latestSnapshot);
                if (similarMoments != null && similarMoments.Any())
                {
                    var sessionSb = new StringBuilder();
                    sessionSb.AppendLine("Similar past moments retrieved from memory:");
                    foreach (var moment in similarMoments)
                    {
                        sessionSb.AppendLine($"- At {moment.Timestamp:g}:");
                        // Summarize slices for brevity
                        var slicesSummary = string.Join(", ", moment.Slices.Select(s => $"{s.SourceType}"));
                        sessionSb.AppendLine($"  Context: {slicesSummary}");
                        // If we had summary text in metadata, we'd use it here.
                    }
                    sessionContext = sessionSb.ToString();
                }
            }

            return new LayeredContextDto(
                ImmediateContext: immediateContext,
                SessionContext: sessionContext,
                ProjectContext: projectContext,
                SeasonalContext: seasonalContext
            );
        }
    }
}
