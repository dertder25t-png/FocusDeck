using System;
using System.Collections.Generic;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using FocusDeck.Domain.Entities.Context;
using Microsoft.AspNetCore.Mvc;

namespace FocusDeck.Server.Controllers.V1.Context
{
    [ApiController]
    [Route("api/v1/context")]
    public class ContextController : ControllerBase
    {
        [HttpPost("snapshots")]
        public Task<IActionResult> CaptureSnapshot()
        {
            // TODO: Implement the logic to capture a new context snapshot.
            // This will involve calling the IContextSnapshotService.
            var snapshot = CreateFakeSnapshot();
            return Task.FromResult<IActionResult>(Ok(snapshot));
        }

        [HttpGet("snapshots/latest")]
        public Task<IActionResult> GetLatestSnapshot()
        {
            // TODO: Implement the logic to retrieve the latest context snapshot.
            // This will involve calling the IContextSnapshotService.
            var snapshot = CreateFakeSnapshot();
            return Task.FromResult<IActionResult>(Ok(snapshot));
        }

        [HttpGet("snapshots/{id}")]
        public Task<IActionResult> GetSnapshot(Guid id)
        {
            // TODO: Implement the logic to retrieve a context snapshot by its ID.
            // This will involve calling the IContextSnapshotService.
            var snapshot = CreateFakeSnapshot(id);
            return Task.FromResult<IActionResult>(Ok(snapshot));
        }

        private ContextSnapshot CreateFakeSnapshot(Guid? id = null)
        {
            return new ContextSnapshot
            {
                Id = id ?? Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                Timestamp = DateTimeOffset.UtcNow,
                Slices = new List<ContextSlice>
                {
                    new ContextSlice
                    {
                        SourceType = ContextSourceType.DesktopActiveWindow,
                        Timestamp = DateTimeOffset.UtcNow,
                        Data = new JsonObject
                        {
                            ["application"] = "Visual Studio Code",
                            ["title"] = "FocusDeck - context_snapshot_pipeline.md"
                        }
                    },
                    new ContextSlice
                    {
                        SourceType = ContextSourceType.GoogleCalendar,
                        Timestamp = DateTimeOffset.UtcNow,
                        Data = new JsonObject
                        {
                            ["event"] = "Team Standup",
                            ["startTime"] = DateTimeOffset.UtcNow.AddMinutes(15).ToString("o"),
                            ["endTime"] = DateTimeOffset.UtcNow.AddMinutes(45).ToString("o")
                        }
                    },
                    new ContextSlice
                    {
                        SourceType = ContextSourceType.CanvasAssignments,
                        Timestamp = DateTimeOffset.UtcNow,
                        Data = new JsonObject
                        {
                            ["assignment"] = "Finish the context snapshot system",
                            ["course"] = "CS 4500",
                            ["dueDate"] = DateTimeOffset.UtcNow.AddDays(2).ToString("o")
                        }
                    }
                },
                Metadata = new ContextSnapshotMetadata
                {
                    DeviceName = "Desktop-12345",
                    OperatingSystem = "Windows 11"
                }
            };
        }
    }
}
