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
    public class SpotifySource : IContextSnapshotSource
    {
        private readonly IHubContext<PrivacyDataHub> _hubContext;
        public string SourceName => "Spotify";

        public SpotifySource(IHubContext<PrivacyDataHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public async Task<ContextSlice?> CaptureAsync(Guid userId, CancellationToken ct)
        {
            // TODO: Implement the logic to capture the user's currently playing Spotify song.
            // This will involve using the Spotify API.
            var data = new JsonObject
            {
                ["artist"] = "Lofi Girl",
                ["track"] = "lofi hip hop radio - beats to relax/study to",
                ["album"] = "Lofi Girl"
            };
            var slice = new ContextSlice
            {
                SourceType = ContextSourceType.Spotify,
                Timestamp = DateTimeOffset.UtcNow,
                Data = data
            };

            // Send the data to the PrivacyDataHub
            await _hubContext.Clients.User(userId.ToString()).SendAsync("ReceivePrivacyData", "Spotify", data.ToJsonString(), ct);

            return slice;
        }
    }
}
