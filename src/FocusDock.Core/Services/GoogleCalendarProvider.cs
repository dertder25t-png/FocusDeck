namespace FocusDock.Core.Services;

using System.Net.Http;
using System.Text.Json;
using FocusDock.Data;
using FocusDock.Data.Models;

/// <summary>
/// Google Calendar API integration via OAuth2
/// </summary>
public class GoogleCalendarProvider
{
    private readonly HttpClient _httpClient;
    private readonly string _clientId;
    private readonly string _clientSecret;
    private readonly string _redirectUri;

    public GoogleCalendarProvider(string clientId, string clientSecret, string redirectUri = "http://localhost:5000/callback")
    {
        _clientId = clientId;
        _clientSecret = clientSecret;
        _redirectUri = redirectUri;
        _httpClient = new HttpClient();
    }

    /// <summary>
    /// Get the OAuth2 authorization URL for user login
    /// </summary>
    public string GetAuthorizationUrl(string state = "state123")
    {
        return $"https://accounts.google.com/o/oauth2/v2/auth?" +
               $"client_id={Uri.EscapeDataString(_clientId)}&" +
               $"redirect_uri={Uri.EscapeDataString(_redirectUri)}&" +
               $"response_type=code&" +
               $"scope={Uri.EscapeDataString("https://www.googleapis.com/auth/calendar.readonly")}&" +
               $"state={state}&" +
               $"access_type=offline&" +
               $"prompt=consent";
    }

    /// <summary>
    /// Exchange authorization code for access token
    /// </summary>
    public async Task<(string accessToken, string refreshToken)?> ExchangeCodeForToken(string code)
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "https://oauth2.googleapis.com/token")
            {
                Content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    { "client_id", _clientId },
                    { "client_secret", _clientSecret },
                    { "redirect_uri", _redirectUri },
                    { "grant_type", "authorization_code" },
                    { "code", code }
                })
            };

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode) return null;

            var content = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(content);
            var root = doc.RootElement;

            var accessToken = root.GetProperty("access_token").GetString();
            var refreshToken = root.TryGetProperty("refresh_token", out var rt) ? rt.GetString() : null;

            if (string.IsNullOrEmpty(accessToken) || string.IsNullOrEmpty(refreshToken))
                return null;

            return (accessToken!, refreshToken!);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Refresh an expired access token using refresh token
    /// </summary>
    public async Task<string?> RefreshAccessToken(string refreshToken)
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "https://oauth2.googleapis.com/token")
            {
                Content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    { "client_id", _clientId },
                    { "client_secret", _clientSecret },
                    { "grant_type", "refresh_token" },
                    { "refresh_token", refreshToken }
                })
            };

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode) return null;

            var content = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(content);
            var accessToken = doc.RootElement.GetProperty("access_token").GetString();

            return accessToken;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Fetch calendar events from Google Calendar
    /// </summary>
    public async Task<List<CalendarEvent>> FetchCalendarEvents(string accessToken, int daysAhead = 30)
    {
        try
        {
            var now = DateTime.UtcNow;
            var timeMin = now.ToString("o");
            var timeMax = now.AddDays(daysAhead).ToString("o");

            var request = new HttpRequestMessage(HttpMethod.Get,
                $"https://www.googleapis.com/calendar/v3/calendars/primary/events?" +
                $"timeMin={Uri.EscapeDataString(timeMin)}&" +
                $"timeMax={Uri.EscapeDataString(timeMax)}&" +
                $"maxResults=25&" +
                $"singleEvents=true&" +
                $"orderBy=startTime");

            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode) return new();

            var content = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(content);
            var events = new List<CalendarEvent>();

            if (doc.RootElement.TryGetProperty("items", out var items))
            {
                foreach (var item in items.EnumerateArray())
                {
                    var evt = new CalendarEvent
                    {
                        Id = item.GetProperty("id").GetString() ?? "",
                        Title = item.GetProperty("summary").GetString() ?? "Untitled",
                        Source = "GoogleCalendar",
                        IsAttending = true
                    };

                    // Parse start time
                    if (item.TryGetProperty("start", out var start))
                    {
                        if (start.TryGetProperty("dateTime", out var dt))
                            evt.StartTime = DateTime.Parse(dt.GetString() ?? DateTime.Now.ToString("o"));
                        else if (start.TryGetProperty("date", out var d))
                            evt.StartTime = DateTime.Parse(d.GetString() ?? DateTime.Now.ToString("o"));
                    }

                    // Parse end time
                    if (item.TryGetProperty("end", out var end))
                    {
                        if (end.TryGetProperty("dateTime", out var dt))
                            evt.EndTime = DateTime.Parse(dt.GetString() ?? DateTime.Now.AddHours(1).ToString("o"));
                        else if (end.TryGetProperty("date", out var d))
                            evt.EndTime = DateTime.Parse(d.GetString() ?? DateTime.Now.AddHours(1).ToString("o"));
                    }

                    events.Add(evt);
                }
            }

            return events;
        }
        catch
        {
            return new();
        }
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}
