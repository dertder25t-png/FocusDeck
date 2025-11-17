using System;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using FocusDeck.Domain.Entities.Context;

namespace FocusDeck.Services.Context.Sources
{
    public class GoogleCalendarSource : IContextSnapshotSource
    {
        public string SourceName => "GoogleCalendar";

        public Task<ContextSlice?> CaptureAsync(Guid userId, CancellationToken ct)
        {
            // TODO: Implement the logic to capture the user's upcoming Google Calendar event.
            // This will involve using the Google Calendar API.
            var data = new JsonObject
            {
                ["event"] = "Team Standup",
                ["startTime"] = DateTimeOffset.UtcNow.AddMinutes(15).ToString("o"),
                ["endTime"] = DateTimeOffset.UtcNow.AddMinutes(45).ToString("o")
            };
            var slice = new ContextSlice
            {
                SourceType = ContextSourceType.GoogleCalendar,
                Timestamp = DateTimeOffset.UtcNow,
                Data = data
            };
            return Task.FromResult<ContextSlice?>(slice);
        }
    }
}
