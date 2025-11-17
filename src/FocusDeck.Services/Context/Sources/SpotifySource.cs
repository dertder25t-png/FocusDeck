using System;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using FocusDeck.Domain.Entities.Context;

namespace FocusDeck.Services.Context.Sources
{
    public class SpotifySource : IContextSnapshotSource
    {
        public string SourceName => "Spotify";

        public Task<ContextSlice?> CaptureAsync(Guid userId, CancellationToken ct)
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
            return Task.FromResult<ContextSlice?>(slice);
        }
    }
}
