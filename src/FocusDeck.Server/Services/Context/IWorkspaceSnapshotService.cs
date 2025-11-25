using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FocusDeck.Domain.Entities;

namespace FocusDeck.Server.Services.Context
{
    public interface IWorkspaceSnapshotService
    {
        Task<WorkspaceSnapshot> CaptureSnapshotAsync(string name, string windowLayoutJson, string? browserSessionId, string? activeNoteId, Guid? projectId);
        Task<List<WorkspaceSnapshot>> GetSnapshotsAsync();
        Task<WorkspaceSnapshot?> GetSnapshotAsync(string id);
        Task DeleteSnapshotAsync(string id);
    }
}
