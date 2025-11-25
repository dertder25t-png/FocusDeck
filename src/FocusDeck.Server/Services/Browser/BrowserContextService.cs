using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using FocusDeck.Domain.Entities;
using FocusDeck.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FocusDeck.Server.Services.Browser
{
    public interface IBrowserContextService
    {
        Task<BrowserSession> ProcessTabSnapshotAsync(string deviceId, List<TabSnapshot> tabs, Guid tenantId);
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

        public async Task<BrowserSession> ProcessTabSnapshotAsync(string deviceId, List<TabSnapshot> tabs, Guid tenantId)
        {
            var session = new BrowserSession
            {
                TenantId = tenantId,
                DeviceId = deviceId,
                CreatedAt = DateTime.UtcNow,
                TabsJson = JsonSerializer.Serialize(tabs)
            };

            _db.BrowserSessions.Add(session);
            await _db.SaveChangesAsync();
            return session;
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
