using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FocusDeck.Persistence;
using FocusDeck.Server.Services.Jarvis.Clustering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FocusDeck.Server.Services.Jarvis
{
    public class PatternRecognitionJob
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<PatternRecognitionJob> _logger;

        public PatternRecognitionJob(IServiceProvider serviceProvider, ILogger<PatternRecognitionJob> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Starting Pattern Recognition Job...");

            using var scope = _serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AutomationDbContext>();
            var clustering = scope.ServiceProvider.GetRequiredService<IBehavioralClusteringService>();
            var generator = scope.ServiceProvider.GetRequiredService<IAutomationGeneratorService>();

            // Iterate over all active users
            // Assuming we have a way to list users, or distinct UserIds from Snapshots
            // For MVP, just get distinct UserIds from recent snapshots
            var recentUserIds = await db.ContextSnapshots
                .Where(s => s.Timestamp >= DateTimeOffset.UtcNow.AddDays(-2))
                .Select(s => s.UserId)
                .Distinct()
                .ToListAsync(cancellationToken);

            foreach (var userId in recentUserIds)
            {
                try
                {
                    var clusters = await clustering.IdentifyClustersAsync(userId.ToString(), DateTime.UtcNow.AddDays(-7), DateTime.UtcNow);

                    foreach (var cluster in clusters)
                    {
                        // Check if we already have a proposal for this "intent" or similar cluster?
                        // For MVP, just generate. The GeneratorService will create a proposal.
                        // Ideally we check if a similar automation already exists.

                        await generator.GenerateProposalAsync(cluster);
                    }

                    _logger.LogInformation("Processed {Count} clusters for user {UserId}", clusters.Count, userId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing patterns for user {UserId}", userId);
                }
            }
        }
    }
}
