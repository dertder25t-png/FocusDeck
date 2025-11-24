using System;
using System.Linq;
using System.Threading.Tasks;
using FocusDeck.Domain.Entities;
using FocusDeck.Persistence;
using FocusDeck.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FocusDeck.Server.Jobs
{
    public class BrowserTabCleanupJob
    {
        private readonly AutomationDbContext _dbContext;
        private readonly IClock _clock;
        private readonly ILogger<BrowserTabCleanupJob> _logger;

        public BrowserTabCleanupJob(
            AutomationDbContext dbContext,
            IClock clock,
            ILogger<BrowserTabCleanupJob> logger)
        {
            _dbContext = dbContext;
            _clock = clock;
            _logger = logger;
        }

        public async Task ExecuteAsync()
        {
            var cutoff = _clock.UtcNow.AddHours(-2);

            // Find stale sessions that are bound to a project
            var staleSessions = await _dbContext.BrowserSessions
                .Where(s => s.LastUpdated < cutoff && s.BoundProjectId != null)
                .ToListAsync();

            if (!staleSessions.Any()) return;

            _logger.LogInformation("Found {Count} stale browser sessions to fold.", staleSessions.Count);

            foreach (var session in staleSessions)
            {
                // Create a Session Bundle
                var bundle = new CapturedItem
                {
                    Id = Guid.NewGuid(),
                    TenantId = session.TenantId,
                    ProjectId = session.BoundProjectId,
                    CapturedAt = session.LastUpdated,
                    Title = $"Auto-Saved Session - {session.LastUpdated:g}",
                    Content = session.TabsJson,
                    Kind = CapturedItemType.SessionBundle,
                    Url = "focusdeck://session/" + session.Id // Virtual URL
                };

                _dbContext.CapturedItems.Add(bundle);

                // Reset the session (simulate closing) or delete it?
                // For now, we clear the tabs to indicate they are "closed" on the device (conceptually)
                // In reality, the device might still have them open, but we consider them "filed away".
                // Or we can delete the session record so a new one starts next time.
                _dbContext.BrowserSessions.Remove(session);
            }

            await _dbContext.SaveChangesAsync();
        }
    }
}
