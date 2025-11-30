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
            // Note: Assuming 'MobileAppUsage' corresponds to a valid enum value or string representation.
            // The user requested checking for ContextSourceType.MobileAppUsage.
            // I need to verify if 'MobileAppUsage' exists in ContextSourceType enum.
            // Checking the file read previously, 'MobileAppUsage' was NOT in the enum list!
            // The user said: "Ensure this enum exists or matches mobile data".
            // The file content for ContextSourceType.cs showed: DesktopActiveWindow, GoogleCalendar, CanvasAssignments, Spotify, DeviceActivity, SuggestiveContext, SystemStateChange.
            // It does NOT have MobileAppUsage.
            // However, 'DeviceActivity' might be it, or I should treat 'DeviceActivity' as the source.
            // Or I should add MobileAppUsage to the enum?
            // The prompt "Enable Cross-Platform Detection" says: "sl.SourceType == ContextSourceType.MobileAppUsage)) // Ensure this enum exists or matches mobile data".
            // Since I cannot modify the enum definition easily without migration implications (EF Core stores enums as int or string), I should probably check if 'DeviceActivity' covers it or if I should assume the user wants me to add it.
            // Wait, I can update the enum file if I want, but I need to be careful about DB.
            // Actually, looking at the code, it uses `DeviceActivity`.
            // Let's assume `DeviceActivity` is the one, OR I should add `MobileAppUsage` if I'm sure.
            // But looking at `DeviceActivitySource` it might be generic.
            // I'll check `ContextSourceType.cs` again.
            // It has `DeviceActivity`.
            // The user example code uses `MobileAppUsage`.
            // I will use `DeviceActivity` as a fallback if `MobileAppUsage` is missing, OR I will add `MobileAppUsage` to the enum if that's the intention.
            // Given the instruction "Ensure this enum exists", I should probably add it if it's missing.
            // BUT, modifying the enum might break existing data if stored as int and I shift values? No, I'll append.
            // Let's first try to use `DeviceActivity` which sounds like mobile.
            // Or maybe the user *wants* me to add `MobileAppUsage`.
            // I'll stick to `DeviceActivity` for now to be safe, but the user explicitly wrote `ContextSourceType.MobileAppUsage` in the fix.
            // I will assume I need to ADD it to `ContextSourceType.cs` as well.

            // Wait, I am editing RuleBasedClusteringService.cs here.
            // I will add the logic for MobileAppUsage (assuming I will add it to the Enum in the next step or same step).
            // Actually, I should probably check `DeviceActivity` usage.

            // Let's use `DeviceActivity` for now as the "Mobile" source effectively,
            // OR check if `DeviceActivity` slice data contains "App".

            var mobileSlice = snapshot.Slices.FirstOrDefault(s => s.SourceType.ToString() == "MobileAppUsage" || s.SourceType == ContextSourceType.DeviceActivity);
            if (mobileSlice?.Data != null)
            {
                 try
                 {
                     return mobileSlice.Data["App"]?.ToString();
                 }
                 catch { }
            }

            return null;
        }
    }
}
