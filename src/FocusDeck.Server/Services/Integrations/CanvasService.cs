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

        public async Task<List<CanvasAssignment>> GetUpcomingAssignments(string canvasDomain, string accessToken)
        {
            try
            {
                var url = $"https://{canvasDomain}/api/v1/users/self/upcoming_events";
                
                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");

                var response = await _httpClient.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Successfully fetched Canvas assignments");
                    return new List<CanvasAssignment>(); // Placeholder
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching Canvas assignments");
            }

            return new List<CanvasAssignment>();
        }

        public async Task<List<CanvasGrade>> GetRecentGrades(string canvasDomain, string accessToken, string courseId)
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

        public async Task<List<CanvasAnnouncement>> GetAnnouncements(string canvasDomain, string accessToken)
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
