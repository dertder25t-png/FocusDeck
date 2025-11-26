using System;
using System.Text.Json.Nodes;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FocusDeck.Domain.Entities.Context;
using FocusDeck.Server.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace FocusDeck.Services.Context.Sources
{
    public class CanvasAssignmentsSource : IContextSnapshotSource
    {
        private readonly IHubContext<PrivacyDataHub> _hubContext;
        public string SourceName => "CanvasAssignments";

        public CanvasAssignmentsSource(IHubContext<PrivacyDataHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public async Task<ContextSlice?> CaptureAsync(Guid userId, CancellationToken ct)
        {
            // TODO: Implement the logic to capture the user's upcoming Canvas assignment.
            // This will involve using the Canvas API.
            var data = new JsonObject
            {
                ["assignment"] = "Finish the context snapshot system",
                ["course"] = "CS 4500",
                ["dueDate"] = DateTimeOffset.UtcNow.AddDays(2).ToString("o")
            };
            var slice = new ContextSlice
            {
                SourceType = ContextSourceType.CanvasAssignments,
                Timestamp = DateTimeOffset.UtcNow,
                Data = data
            };

            // Send the data to the PrivacyDataHub
            await _hubContext.Clients.User(userId.ToString()).SendAsync("ReceivePrivacyData", "CanvasAssignments", data.ToJsonString(), ct);

            return slice;
        }
    }
}
