namespace FocusDeck.Server.Services.Integrations
{
    /// <summary>
    /// Service for integrating with Canvas LMS API
    /// </summary>
    public class CanvasService
    {
        private readonly ILogger<CanvasService> _logger;
        private readonly HttpClient _httpClient;

        public CanvasService(ILogger<CanvasService> logger)
        {
            _logger = logger;
            _httpClient = new HttpClient();
        }

        public virtual async Task<List<CanvasAssignment>> GetUpcomingAssignments(string canvasDomain, string accessToken)
        {
            try
            {
                var url = $"https://{canvasDomain}/api/v1/users/self/upcoming_events";
                
                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");

                var response = await _httpClient.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    // Minimal parse: map items with assignment-like shape
                    var items = System.Text.Json.JsonDocument.Parse(json).RootElement;
                    var list = new List<CanvasAssignment>();
                    foreach (var e in items.EnumerateArray())
                    {
                        // Upcoming events can be assignments or other events
                        var id = e.TryGetProperty("assignment_id", out var aid) && aid.ValueKind != System.Text.Json.JsonValueKind.Null
                            ? aid.GetRawText().Trim('"')
                            : (e.TryGetProperty("id", out var eid) ? eid.ToString() : Guid.NewGuid().ToString());
                        var name = e.TryGetProperty("title", out var t) ? t.GetString() ?? "Untitled" : "Untitled";
                        DateTime? dueAt = null;
                        if (e.TryGetProperty("due_at", out var due) && due.ValueKind == System.Text.Json.JsonValueKind.String)
                        {
                            if (DateTime.TryParse(due.GetString(), out var dt)) dueAt = dt.ToUniversalTime();
                        }
                        string courseId = e.TryGetProperty("context_code", out var cc) ? cc.GetString() ?? string.Empty : string.Empty;
                        string courseName = e.TryGetProperty("context_name", out var cn) ? cn.GetString() ?? string.Empty : string.Empty;

                        list.Add(new CanvasAssignment
                        {
                            Id = id,
                            Name = name,
                            DueAt = dueAt,
                            CourseId = courseId,
                            CourseName = courseName
                        });
                    }
                    _logger.LogInformation("Fetched {Count} Canvas upcoming events", list.Count);
                    return list;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching Canvas assignments");
            }

            return new List<CanvasAssignment>();
        }

        public virtual async Task<List<CanvasGrade>> GetRecentGrades(string canvasDomain, string accessToken, string courseId)
        {
            try
            {
                var url = $"https://{canvasDomain}/api/v1/courses/{courseId}/assignments";
                
                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");

                var response = await _httpClient.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Successfully fetched Canvas grades");
                    return new List<CanvasGrade>(); // Placeholder
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching Canvas grades");
            }

            return new List<CanvasGrade>();
        }

        public virtual async Task<List<CanvasAnnouncement>> GetAnnouncements(string canvasDomain, string accessToken)
        {
            try
            {
                var url = $"https://{canvasDomain}/api/v1/announcements";
                
                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");

                var response = await _httpClient.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Successfully fetched Canvas announcements");
                    return new List<CanvasAnnouncement>(); // Placeholder
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching Canvas announcements");
            }

            return new List<CanvasAnnouncement>();
        }
    }

    public class CanvasAssignment
    {
        public string Id { get; set; } = null!;
        public string Name { get; set; } = null!;
        public DateTime? DueAt { get; set; }
        public string CourseId { get; set; } = null!;
        public string CourseName { get; set; } = null!;
    }

    public class CanvasGrade
    {
        public string AssignmentId { get; set; } = null!;
        public string AssignmentName { get; set; } = null!;
        public double? Score { get; set; }
        public string? Grade { get; set; }
        public DateTime? GradedAt { get; set; }
    }

    public class CanvasAnnouncement
    {
        public string Id { get; set; } = null!;
        public string Title { get; set; } = null!;
        public string Message { get; set; } = null!;
        public DateTime PostedAt { get; set; }
        public string CourseId { get; set; } = null!;
    }
}
