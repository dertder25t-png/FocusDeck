using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FocusDeck.Domain.Entities;
using FocusDeck.Persistence;
using FocusDeck.SharedKernel.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace FocusDeck.Server.Services.Context
{
    public class WorkspaceSnapshotService : IWorkspaceSnapshotService
    {
        private readonly AutomationDbContext _dbContext;
        private readonly ICurrentTenant _currentTenant;

        public WorkspaceSnapshotService(AutomationDbContext dbContext, ICurrentTenant currentTenant)
        {
            _dbContext = dbContext;
            _currentTenant = currentTenant;
        }

        public async Task<WorkspaceSnapshot> CaptureSnapshotAsync(string name, string windowLayoutJson, string? browserSessionId, string? activeNoteId, Guid? projectId)
        {
            var snapshot = new WorkspaceSnapshot
            {
                Name = name,
                WindowLayoutJson = windowLayoutJson,
                BrowserSessionId = browserSessionId,
                ActiveNoteId = activeNoteId,
                ProjectId = projectId,
                CreatedAt = DateTime.UtcNow
            };

            // TenantId will be set by AutomationDbContext if using SaveChanges()

            _dbContext.WorkspaceSnapshots.Add(snapshot);
            await _dbContext.SaveChangesAsync();
            return snapshot;
        }

        public async Task<List<WorkspaceSnapshot>> GetSnapshotsAsync()
        {
            return await _dbContext.WorkspaceSnapshots
                .Include(s => s.BrowserSession)
                .Include(s => s.ActiveNote)
                .Include(s => s.Project)
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();
        }

        public async Task<WorkspaceSnapshot?> GetSnapshotAsync(string id)
        {
            return await _dbContext.WorkspaceSnapshots
                .Include(s => s.BrowserSession)
                .Include(s => s.ActiveNote)
                .Include(s => s.Project)
                .FirstOrDefaultAsync(s => s.Id == id);
        }

        public async Task DeleteSnapshotAsync(string id)
        {
            var snapshot = await _dbContext.WorkspaceSnapshots.FindAsync(id);
            if (snapshot != null)
            {
                _dbContext.WorkspaceSnapshots.Remove(snapshot);
                await _dbContext.SaveChangesAsync();
            }
        }
    }
}
