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
    public class DeviceActivitySource : IContextSnapshotSource
    {
        private readonly IHubContext<PrivacyDataHub> _hubContext;
        public string SourceName => "DeviceActivity";

        public DeviceActivitySource(IHubContext<PrivacyDataHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public async Task<ContextSlice?> CaptureAsync(Guid userId, CancellationToken ct)
        {
            // TODO: Implement the logic to capture the user's device activity.
            // This will involve monitoring system-level events.
            var data = new JsonObject
            {
                ["status"] = "Active",
                ["lastInputTime"] = DateTimeOffset.UtcNow.ToString("o")
            };
            var slice = new ContextSlice
            {
                SourceType = ContextSourceType.DeviceActivity,
                Timestamp = DateTimeOffset.UtcNow,
                Data = data
            };

            // Send the data to the PrivacyDataHub
            await _hubContext.Clients.User(userId.ToString()).SendAsync("ReceivePrivacyData", "DeviceActivity", data.ToJsonString(), ct);

            return slice;
        }
    }
}
