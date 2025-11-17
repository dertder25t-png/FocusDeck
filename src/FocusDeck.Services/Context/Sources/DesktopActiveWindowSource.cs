using System;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using FocusDeck.Domain.Entities.Context;

namespace FocusDeck.Services.Context.Sources
{
    public class DesktopActiveWindowSource : IContextSnapshotSource
    {
        public string SourceName => "DesktopActiveWindow";

        public Task<ContextSlice?> CaptureAsync(Guid userId, CancellationToken ct)
        {
            // TODO: Implement the logic to capture the active window on the user's desktop.
            // This will involve platform-specific code to get the window title and process name.
            var data = new JsonObject
            {
                ["application"] = "Visual Studio Code",
                ["title"] = "FocusDeck - context_snapshot_pipeline.md"
            };
            var slice = new ContextSlice
            {
                SourceType = ContextSourceType.DesktopActiveWindow,
                Timestamp = DateTimeOffset.UtcNow,
                Data = data
            };
            return Task.FromResult<ContextSlice?>(slice);
        }
    }
}
