using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FocusDeck.Contracts.Repositories;
using FocusDeck.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FocusDeck.Persistence.Repositories
{
    public class EfEventCacheRepository : IEventCacheRepository
    {
        private readonly AutomationDbContext _db;

        public EfEventCacheRepository(AutomationDbContext db)
        {
            _db = db;
        }

        public async Task<IEnumerable<EventCache>> GetActiveEventsAsync(Guid userId, DateTimeOffset timestamp, CancellationToken ct = default)
        {
            var ts = timestamp.UtcDateTime;

            // Note: We rely on Global Query Filters for TenantId scoping.
            // We do not filter by UserId because CalendarSource currently lacks a UserId property.
            // This assumes Tenant ~= User or CalendarSource is shared within Tenant.

            return await _db.EventCache
                .Include(e => e.CalendarSource)
                .Where(e => e.StartTime <= ts && e.EndTime >= ts)
                .ToListAsync(ct);
        }
    }
}
