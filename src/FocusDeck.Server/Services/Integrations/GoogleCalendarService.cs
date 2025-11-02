using FocusDeck.Domain.Entities.Automations;

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
                    // Parse and return events
                    _logger.LogInformation("Successfully fetched calendar events");
                    return new List<CalendarEvent>(); // Placeholder
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching Google Calendar events");
            }

            return new List<CalendarEvent>();
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
                    System.Text.Json.JsonSerializer.Serialize(eventData),
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
