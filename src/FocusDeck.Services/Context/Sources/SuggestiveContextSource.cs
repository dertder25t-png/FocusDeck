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
    public class SuggestiveContextSource : IContextSnapshotSource
    {
        private readonly IHubContext<PrivacyDataHub> _hubContext;
        public string SourceName => "SuggestiveContext";

        public SuggestiveContextSource(IHubContext<PrivacyDataHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public async Task<ContextSlice?> CaptureAsync(Guid userId, CancellationToken ct)
        {
            // TODO: Implement the logic to generate a suggestive context slice.
            // This will involve using an AI model to analyze the user's current context and suggest relevant information.
            var data = new JsonObject
            {
                ["suggestion"] = "Take a break and stretch."
            };
            var slice = new ContextSlice
            {
                SourceType = ContextSourceType.SuggestiveContext,
                Timestamp = DateTimeOffset.UtcNow,
                Data = data
            };

            // Send the data to the PrivacyDataHub
            await _hubContext.Clients.User(userId.ToString()).SendAsync("ReceivePrivacyData", "SuggestiveContext", data.ToJsonString(), ct);

            return slice;
        }
    }
}
