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
        private readonly FocusDeck.Server.Services.Jarvis.IProjectSortingService _sortingService;

        public BrowserContextService(AutomationDbContext db, FocusDeck.Server.Services.Jarvis.IProjectSortingService sortingService)
        {
            _db = db;
            _sortingService = sortingService;
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

            if (!item.ProjectId.HasValue)
            {
                await _sortingService.SortItemAsync(item, CancellationToken.None);
            }

            _db.CapturedItems.Add(item);
            await _db.SaveChangesAsync();

            return item.Id;
        }
    }
}
