using System;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using FocusDeck.Contracts.Repositories;
using FocusDeck.Domain.Entities.Context;

namespace FocusDeck.Services.Context.Sources
{
    public class GoogleCalendarSource : IContextSnapshotSource
    {
        private readonly IEventCacheRepository _repository;

        public string SourceName => "GoogleCalendar";

        public GoogleCalendarSource(IEventCacheRepository repository)
        {
            _repository = repository;
        }

        public async Task<ContextSlice?> CaptureAsync(Guid userId, CancellationToken ct)
        {
            var now = DateTimeOffset.UtcNow;
            var activeEvents = await _repository.GetActiveEventsAsync(userId, now, ct);
            var currentEvent = activeEvents.FirstOrDefault(); // Take first if multiple

            if (currentEvent == null)
            {
                return null;
            }

            var data = new JsonObject
            {
                ["event"] = currentEvent.Title,
                ["startTime"] = currentEvent.StartTime.ToString("o"),
                ["endTime"] = currentEvent.EndTime.ToString("o"),
                ["location"] = currentEvent.Location,
                ["description"] = currentEvent.Description,
                ["eventId"] = currentEvent.ExternalEventId,
                ["calendarName"] = currentEvent.CalendarSource?.Name
            };

            return new ContextSlice
            {
                Id = Guid.NewGuid(),
                SourceType = ContextSourceType.GoogleCalendar,
                Timestamp = now,
                Data = data
            };
        }
    }
}
