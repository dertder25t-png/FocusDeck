using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using FocusDeck.Domain.Entities.Automations;
using Microsoft.Extensions.Logging;

namespace FocusDeck.Server.Services.Integrations
{
    /// <summary>
    /// Service for integrating with Google Calendar API
    /// </summary>
    public class GoogleCalendarService
    {
        private readonly ILogger<GoogleCalendarService> _logger;
        private readonly HttpClient _httpClient;

        public GoogleCalendarService(ILogger<GoogleCalendarService> logger)
        {
            _logger = logger;
            _httpClient = new HttpClient();
        }

        public async Task<List<CalendarEvent>> GetUpcomingEvents(string accessToken, int minutesAhead = 30)
        {
            try
            {
                var now = DateTime.UtcNow;
                var timeMin = now.ToString("yyyy-MM-ddTHH:mm:ssZ");
                var timeMax = now.AddMinutes(minutesAhead).ToString("yyyy-MM-ddTHH:mm:ssZ");

                var url = $"https://www.googleapis.com/calendar/v3/calendars/primary/events" +
                         $"?timeMin={timeMin}&timeMax={timeMax}&singleEvents=true&orderBy=startTime";

                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");

                var response = await _httpClient.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var list = JsonSerializer.Deserialize<GoogleEventList>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return list?.Items?.Select(MapToEntity).ToList() ?? new List<CalendarEvent>();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching Google Calendar events");
            }

            return new List<CalendarEvent>();
        }

        public async Task<(List<CalendarEvent> Events, string NextSyncToken)> SyncDeltaAsync(string accessToken, string? syncToken)
        {
            // If no sync token, do a full sync from now? Or from a specific time?
            // Usually valid sync tokens allow listing without timeMin if we want *all* changes.
            // But if syncToken is null, we usually want to start fresh or list from now.
            // Let's assume if null, we do a full list from now (or a reasonable lookback) and get a sync token.

            // However, the Google Calendar API documentation says:
            // "Perform an initial full sync of the calendar's events. The result of the list request will contain a nextSyncToken."
            // "Use the nextSyncToken in a subsequent list request to retrieve the changes."

            var url = "https://www.googleapis.com/calendar/v3/calendars/primary/events?singleEvents=true"; // singleEvents=true expands recurring events

            if (!string.IsNullOrEmpty(syncToken))
            {
                url += $"&syncToken={syncToken}";
            }
            else
            {
                // Initial sync: fetch future events? Or all?
                // Let's limit to 30 days back to avoid huge payloads if it's a fresh sync for this context
                 var timeMin = DateTime.UtcNow.AddDays(-30).ToString("yyyy-MM-ddTHH:mm:ssZ");
                 url += $"&timeMin={timeMin}";
            }

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");

            try
            {
                var response = await _httpClient.GetAsync(url);

                if (response.StatusCode == HttpStatusCode.Gone) // 410 Gone -> Invalid Sync Token
                {
                    _logger.LogWarning("Sync token is invalid (410 Gone). Performing full sync.");
                    return await SyncDeltaAsync(accessToken, null); // Recursive call without sync token
                }

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var list = JsonSerializer.Deserialize<GoogleEventList>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    var events = list?.Items?.Select(MapToEntity).ToList() ?? new List<CalendarEvent>();
                    return (events, list?.NextSyncToken ?? string.Empty);
                }

                _logger.LogError("Failed to sync calendar events. Status: {Status}", response.StatusCode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception during calendar sync");
            }

            return (new List<CalendarEvent>(), string.Empty);
        }

        public async Task<bool> CreateEvent(string accessToken, string summary, DateTime start, DateTime end)
        {
            try
            {
                var url = "https://www.googleapis.com/calendar/v3/calendars/primary/events";
                
                var eventData = new
                {
                    summary = summary,
                    start = new { dateTime = start.ToString("yyyy-MM-ddTHH:mm:ssZ") },
                    end = new { dateTime = end.ToString("yyyy-MM-ddTHH:mm:ssZ") }
                };

                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");

                var content = new StringContent(
                    JsonSerializer.Serialize(eventData),
                    System.Text.Encoding.UTF8,
                    "application/json"
                );

                var response = await _httpClient.PostAsync(url, content);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating calendar event");
                return false;
            }
        }

        private CalendarEvent MapToEntity(GoogleEventItem item)
        {
             // Handle "date" only events vs "dateTime"
             DateTime start = item.Start?.DateTime ??
                              (DateTime.TryParse(item.Start?.Date, out var d) ? d : DateTime.MinValue);
             DateTime end = item.End?.DateTime ??
                            (DateTime.TryParse(item.End?.Date, out var d2) ? d2 : DateTime.MinValue);

            return new CalendarEvent
            {
                Id = item.Id ?? Guid.NewGuid().ToString(),
                Summary = item.Summary ?? "(No Title)",
                Start = start,
                End = end,
                Location = item.Location,
                Description = item.Description
            };
        }

        // Internal DTOs for deserialization
        private class GoogleEventList
        {
            public string? NextSyncToken { get; set; }
            public List<GoogleEventItem>? Items { get; set; }
        }

        private class GoogleEventItem
        {
            public string? Id { get; set; }
            public string? Summary { get; set; }
            public string? Description { get; set; }
            public string? Location { get; set; }
            public GoogleDate? Start { get; set; }
            public GoogleDate? End { get; set; }
        }

        private class GoogleDate
        {
            public DateTime? DateTime { get; set; }
            public string? Date { get; set; } // For all-day events "yyyy-MM-dd"
        }
    }

    public class CalendarEvent
    {
        public string Id { get; set; } = null!;
        public string Summary { get; set; } = null!;
        public DateTime Start { get; set; }
        public DateTime End { get; set; }
        public string? Location { get; set; }
        public string? Description { get; set; }
    }
}
