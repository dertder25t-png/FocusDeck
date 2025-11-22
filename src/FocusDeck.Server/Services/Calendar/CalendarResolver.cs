using FocusDeck.Domain.Entities;
using FocusDeck.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FocusDeck.Server.Services.Calendar
{
    public class CalendarResolver
    {
        private readonly AutomationDbContext _db;

        public CalendarResolver(AutomationDbContext db)
        {
            _db = db;
        }

        public async Task<EventCache?> ResolveCurrentEventAsync(Guid tenantId)
        {
            var now = DateTime.UtcNow;
            var windowStart = now.AddMinutes(-15);
            var windowEnd = now.AddMinutes(10);

            // Find event happening now or starting soon
            var currentEvent = await _db.EventCache
                .Where(e => e.TenantId == tenantId)
                .Where(e => e.StartTime <= windowEnd && e.EndTime >= windowStart)
                .OrderBy(e => e.StartTime)
                .FirstOrDefaultAsync();

            return currentEvent;
        }
    }
}
