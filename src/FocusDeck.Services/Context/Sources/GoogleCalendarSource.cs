using System;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using FocusDeck.Contracts.Repositories;
using FocusDeck.Domain.Entities.Context;
using FocusDeck.Server.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace FocusDeck.Services.Context.Sources
{
    public class GoogleCalendarSource : IContextSnapshotSource
    {
        private readonly IEventCacheRepository _repository;
        private readonly IHubContext<PrivacyDataHub> _hubContext;

        public string SourceName => "GoogleCalendar";

        public GoogleCalendarSource(IEventCacheRepository repository, IHubContext<PrivacyDataHub> hubContext)
        {
            _repository = repository;
            _hubContext = hubContext;
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

            // Send the data to the PrivacyDataHub
            await _hubContext.Clients.User(userId.ToString()).SendAsync("ReceivePrivacyData", "GoogleCalendar", JsonSerializer.Serialize(currentEvent), ct);

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
