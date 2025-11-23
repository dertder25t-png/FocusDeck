using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FocusDeck.Domain.Entities.Context;
using FocusDeck.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FocusDeck.Server.Services.Jarvis.Clustering
{
    public interface IBehavioralClusteringService
    {
        Task<List<List<ContextSnapshot>>> IdentifyClustersAsync(string userId, DateTime startTime, DateTime endTime);
    }

    public class RuleBasedClusteringService : IBehavioralClusteringService
    {
        private readonly AutomationDbContext _dbContext;
        private readonly ILogger<RuleBasedClusteringService> _logger;

        public RuleBasedClusteringService(AutomationDbContext dbContext, ILogger<RuleBasedClusteringService> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task<List<List<ContextSnapshot>>> IdentifyClustersAsync(string userId, DateTime startTime, DateTime endTime)
        {
            // Fetch snapshots in range
            var startDto = new DateTimeOffset(startTime);
            var endDto = new DateTimeOffset(endTime);

            // Assuming userId is stored as Guid in DB for Snapshot.UserId based on previous file reads
            // But checking ContextSnapshot.cs, UserId is Guid.
            if (!Guid.TryParse(userId, out var userGuid))
            {
                _logger.LogWarning("Invalid user ID for clustering: {UserId}", userId);
                return new List<List<ContextSnapshot>>();
            }

            var snapshots = await _dbContext.ContextSnapshots
                .Include(s => s.Slices)
                .Where(s => s.UserId == userGuid && s.Timestamp >= startDto && s.Timestamp <= endDto)
                .OrderBy(s => s.Timestamp)
                .ToListAsync();

            if (snapshots.Count == 0) return new List<List<ContextSnapshot>>();

            var clusters = new List<List<ContextSnapshot>>();

            // Simple Heuristic: Group by App Open events
            var appOpenSnapshots = snapshots
                .Where(s => s.Slices.Any(sl => sl.SourceType == ContextSourceType.DesktopActiveWindow))
                .ToList();

            // Group by App Name (from slice data)
            var groupedByApp = appOpenSnapshots
                .GroupBy(s => GetAppName(s))
                .Where(g => !string.IsNullOrEmpty(g.Key) && g.Count() >= 3) // Need at least 3 occurrences to form a pattern
                .ToList();

            foreach (var group in groupedByApp)
            {
                // Further refine: Check if they happen around the same time of day (+/- 1 hour)
                var timeGroups = group.GroupBy(s => s.Timestamp.Hour)
                    .Where(tg => tg.Count() >= 3);

                foreach (var timeGroup in timeGroups)
                {
                     clusters.Add(timeGroup.ToList());
                }
            }

            return clusters;
        }

        private string? GetAppName(ContextSnapshot snapshot)
        {
            var slice = snapshot.Slices.FirstOrDefault(s => s.SourceType == ContextSourceType.DesktopActiveWindow);
            if (slice?.Data == null) return null;

            try
            {
                 // Try parsing JSON or accessing Dictionary if mapped?
                 // ContextSlice.Data is JsonObject (System.Text.Json.Nodes)
                 return slice.Data["App"]?.ToString() ??
                        slice.Data["ActiveApplication"]?.ToString() ??
                        slice.Data["Application"]?.ToString();
            }
            catch
            {
                return null;
            }
        }
    }
}
