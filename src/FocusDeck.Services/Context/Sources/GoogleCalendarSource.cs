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
            // TODO: Replace with real Google Calendar API call
            // For now, return a stubbed event
            var data = new JsonObject
            {
                ["event"] = "Team Standup",
                ["startTime"] = DateTimeOffset.UtcNow.AddMinutes(15).ToString("o"),
                ["endTime"] = DateTimeOffset.UtcNow.AddMinutes(45).ToString("o"),
                ["location"] = "Google Meet",
                ["description"] = "Daily sync with the team."
            };

            var slice = new ContextSlice
            {
                Id = Guid.NewGuid(),
                SourceType = ContextSourceType.GoogleCalendar,
                Timestamp = DateTimeOffset.UtcNow,
                Data = data
            };

            return Task.FromResult<ContextSlice?>(slice);
        }
    }
}
