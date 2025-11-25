using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FocusDeck.Domain.Entities;
using FocusDeck.Server.Services.Context;
using Microsoft.AspNetCore.Mvc;

namespace FocusDeck.Server.Controllers.v1
{
    [ApiController]
    [Route("v1/workspaces")]
    public class WorkspacesController : ControllerBase
    {
        private readonly IWorkspaceSnapshotService _snapshotService;

        public WorkspacesController(IWorkspaceSnapshotService snapshotService)
        {
            _snapshotService = snapshotService;
        }

        [HttpPost("snapshots")]
        public async Task<ActionResult<WorkspaceSnapshot>> CaptureSnapshot([FromBody] CaptureSnapshotRequest request)
        {
            var snapshot = await _snapshotService.CaptureSnapshotAsync(
                request.Name,
                request.WindowLayoutJson,
                request.BrowserSessionId,
                request.ActiveNoteId,
                request.ProjectId);
            return Ok(snapshot);
        }

        [HttpGet("snapshots")]
        public async Task<ActionResult<List<WorkspaceSnapshot>>> GetSnapshots()
        {
            var snapshots = await _snapshotService.GetSnapshotsAsync();
            return Ok(snapshots);
        }

        [HttpGet("snapshots/{id}")]
        public async Task<ActionResult<WorkspaceSnapshot>> GetSnapshot(string id)
        {
            var snapshot = await _snapshotService.GetSnapshotAsync(id);
            if (snapshot == null) return NotFound();
            return Ok(snapshot);
        }

        [HttpDelete("snapshots/{id}")]
        public async Task<IActionResult> DeleteSnapshot(string id)
        {
            await _snapshotService.DeleteSnapshotAsync(id);
            return NoContent();
        }
    }

    public class CaptureSnapshotRequest
    {
        public string Name { get; set; } = "Untitled";
        public string WindowLayoutJson { get; set; } = "{}";
        public string? BrowserSessionId { get; set; }
        public string? ActiveNoteId { get; set; }
        public Guid? ProjectId { get; set; }
    }
}
