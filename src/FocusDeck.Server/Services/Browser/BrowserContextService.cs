using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FocusDeck.Domain.Entities;
using FocusDeck.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FocusDeck.Server.Services.Browser
{
    public interface IBrowserContextService
    {
        Task ProcessTabSnapshotAsync(string deviceId, List<TabSnapshot> tabs, Guid tenantId);
        Task<Guid> CaptureItemAsync(CapturedItem item);
    }

    public class TabSnapshot
    {
        public string Url { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }

    public class BrowserContextService : IBrowserContextService
    {
        private readonly AutomationDbContext _db;

        public BrowserContextService(AutomationDbContext db)
        {
            _db = db;
        }

        public async Task ProcessTabSnapshotAsync(string deviceId, List<TabSnapshot> tabs, Guid tenantId)
        {
            // In a real implementation, this would update a "BrowserSession" entity
            // or track active tabs for context. For MVP, we might just log or store transiently.
            // Let's create/update a ContextSnapshot source for this.

            // For now, we'll just log it as a stub implementation
            await Task.CompletedTask;
        }

        public async Task<Guid> CaptureItemAsync(CapturedItem item)
        {
            item.Id = Guid.NewGuid();
            item.CapturedAt = DateTime.UtcNow;

            _db.CapturedItems.Add(item);
            await _db.SaveChangesAsync();

            return item.Id;
        }
    }
}
