using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
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
        Task BindSessionAsync(string deviceId, Guid projectId, Guid tenantId);
        Task<CapturedItem?> GetRestoreSessionAsync(Guid projectId, Guid tenantId);
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
            var session = await _db.BrowserSessions
                .FirstOrDefaultAsync(s => s.DeviceId == deviceId && s.TenantId == tenantId);

            if (session == null)
            {
                session = new BrowserSession
                {
                    Id = Guid.NewGuid().ToString(),
                    DeviceId = deviceId,
                    TenantId = tenantId
                };
                _db.BrowserSessions.Add(session);
            }

            session.TabsJson = JsonSerializer.Serialize(tabs);
            session.LastUpdated = DateTime.UtcNow;

            await _db.SaveChangesAsync();
        }

        public async Task<Guid> CaptureItemAsync(CapturedItem item)
        {
            item.Id = Guid.NewGuid();
            item.CapturedAt = DateTime.UtcNow;

            _db.CapturedItems.Add(item);
            await _db.SaveChangesAsync();

            return item.Id;
        }

        public async Task BindSessionAsync(string deviceId, Guid projectId, Guid tenantId)
        {
            var session = await _db.BrowserSessions
                .FirstOrDefaultAsync(s => s.DeviceId == deviceId && s.TenantId == tenantId);

            if (session == null)
            {
                // Create a session placeholder if it doesn't exist yet
                session = new BrowserSession
                {
                    Id = Guid.NewGuid().ToString(),
                    DeviceId = deviceId,
                    TenantId = tenantId,
                    TabsJson = "[]",
                    LastUpdated = DateTime.UtcNow
                };
                _db.BrowserSessions.Add(session);
            }

            session.BoundProjectId = projectId;
            await _db.SaveChangesAsync();
        }

        public async Task<CapturedItem?> GetRestoreSessionAsync(Guid projectId, Guid tenantId)
        {
            // Find the most recent "SessionBundle" captured item for this project
            return await _db.CapturedItems
                .Where(c => c.TenantId == tenantId && c.ProjectId == projectId && c.Kind == CapturedItemType.SessionBundle)
                .OrderByDescending(c => c.CapturedAt)
                .FirstOrDefaultAsync();
        }
    }
}
