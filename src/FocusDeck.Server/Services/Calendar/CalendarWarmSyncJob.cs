using FocusDeck.Domain.Entities;
using FocusDeck.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FocusDeck.Server.Services.Calendar
{
    public class CalendarWarmSyncJob
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<CalendarWarmSyncJob> _logger;

        public CalendarWarmSyncJob(IServiceProvider serviceProvider, ILogger<CalendarWarmSyncJob> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AutomationDbContext>();

            var sources = await db.CalendarSources.ToListAsync(cancellationToken);
            foreach (var source in sources)
            {
                try
                {
                    await SyncSource(source, db, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to sync calendar source {Id}", source.Id);
                }
            }
        }

        private async Task SyncSource(CalendarSource source, AutomationDbContext db, CancellationToken cancellationToken)
        {
            // TODO: Implement actual sync logic with Google/Outlook APIs
            // For now, just update LastSync timestamp
            source.LastSync = DateTime.UtcNow;
            await db.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Synced calendar source {Name}", source.Name);
        }
    }
}
