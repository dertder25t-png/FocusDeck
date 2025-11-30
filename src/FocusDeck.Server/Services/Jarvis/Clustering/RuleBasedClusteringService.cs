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

            // Look for sessions: Activity gaps > 5 minutes imply a new "session"
            var sessions = new List<List<ContextSnapshot>>();
            var currentSession = new List<ContextSnapshot>();

            foreach (var snap in snapshots)
            {
                if (currentSession.Count > 0 && (snap.Timestamp - currentSession.Last().Timestamp).TotalMinutes > 5)
                {
                    sessions.Add(currentSession);
                    currentSession = new List<ContextSnapshot>();
                }
                currentSession.Add(snap);
            }
            if (currentSession.Count > 0) sessions.Add(currentSession);

            // Analyze sessions for recurring sequences (Simplified Association Rule Learning)
            // Example: If A and B appear in the same session > 3 times
            var sequenceCounts = new Dictionary<string, int>();
            var sequenceExamples = new Dictionary<string, List<ContextSnapshot>>();

            foreach (var session in sessions)
            {
                var appNames = session
                    .Select(s => GetAppName(s))
                    .Where(n => !string.IsNullOrEmpty(n))
                    .Distinct()
                    .OrderBy(n => n)
                    .ToList();

                if (appNames.Count >= 2)
                {
                    var key = string.Join(" + ", appNames); // e.g. "Spotify + VS Code"
                    if (!sequenceCounts.ContainsKey(key)) { sequenceCounts[key] = 0; sequenceExamples[key] = new List<ContextSnapshot>(); }
                    sequenceCounts[key]++;
                    sequenceExamples[key].AddRange(session.Take(5)); // Store samples for the prompt
                }
            }

            // Return clusters that happen frequently (e.g., > 3 times)
            foreach (var seq in sequenceCounts.Where(k => k.Value >= 3))
            {
                clusters.Add(sequenceExamples[seq.Key]);
            }

            return clusters;
        }

        private string? GetAppName(ContextSnapshot snapshot)
        {
            // Check Desktop
            var desktopSlice = snapshot.Slices.FirstOrDefault(s => s.SourceType == ContextSourceType.DesktopActiveWindow);
            if (desktopSlice?.Data != null)
            {
                try
                {
                    return desktopSlice.Data["App"]?.ToString() ??
                           desktopSlice.Data["ActiveApplication"]?.ToString() ??
                           desktopSlice.Data["Application"]?.ToString();
                }
                catch { }
            }

            // Check Mobile
            // Using explicit Enum member now that it is defined
            var mobileSlice = snapshot.Slices.FirstOrDefault(s =>
                s.SourceType == ContextSourceType.DeviceActivity ||
                s.SourceType == ContextSourceType.MobileAppUsage);

            if (mobileSlice?.Data != null)
            {
                 try
                 {
                     // Robust property checking for mobile data structures
                     return mobileSlice.Data["App"]?.ToString() ??
                            mobileSlice.Data["AppName"]?.ToString() ??
                            mobileSlice.Data["Application"]?.ToString();
                 }
                 catch { }
            }

            return null;
        }
    }
}
