using System.Linq;
using System.Text.Json;
using FocusDeck.Services.Abstractions;

namespace FocusDeck.Server.Services.Integrations
{
    /// <summary>
    /// Service for integrating with Canvas LMS API
    /// </summary>
    public class CanvasService : ICanvasService
    {
        private readonly ILogger<CanvasService> _logger;
        private readonly HttpClient _httpClient;

        public CanvasService(ILogger<CanvasService> logger, HttpClient? httpClient = null)
        {
            _logger = logger;
            _httpClient = httpClient ?? new HttpClient();
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
                    var items = JsonDocument.Parse(json).RootElement;
                    var list = new List<CanvasAssignment>();
                    foreach (var e in items.EnumerateArray())
                    {
                        // Upcoming events can be assignments or other events
                        var id = e.TryGetProperty("assignment_id", out var aid) && aid.ValueKind != JsonValueKind.Null
                            ? aid.GetRawText().Trim('"')
                            : (e.TryGetProperty("id", out var eid) ? eid.ToString() : Guid.NewGuid().ToString());
                        var name = e.TryGetProperty("title", out var t) ? t.GetString() ?? "Untitled" : "Untitled";
                        DateTime? dueAt = null;
                        if (e.TryGetProperty("due_at", out var due) && due.ValueKind == JsonValueKind.String && DateTime.TryParse(due.GetString(), out var dt))
                        {
                            dueAt = dt.ToUniversalTime();
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
                // Fetch recent submissions for the student in the specified course
                // include[]=assignment to get assignment names
                // order=graded_at&descending=true to get recent ones
                var url = $"https://{canvasDomain}/api/v1/courses/{courseId}/students/submissions?student_ids[]=self&include[]=assignment&order=graded_at&descending=true&per_page=10";
                
                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");

                var response = await _httpClient.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var items = JsonDocument.Parse(json).RootElement;
                    var list = new List<CanvasGrade>();

                    foreach (var s in items.EnumerateArray())
                    {
                        if (!s.TryGetProperty("grade", out var gradeElement) || gradeElement.ValueKind == JsonValueKind.Null)
                            continue;
                            
                        var grade = gradeElement.ToString();
                        var score = s.TryGetProperty("score", out var sc) && sc.ValueKind == JsonValueKind.Number ? sc.GetDouble() : (double?)null;
                        var gradedAt = s.TryGetProperty("graded_at", out var ga) && ga.ValueKind == JsonValueKind.String
                            && DateTime.TryParse(ga.GetString(), out var dt) ? dt.ToUniversalTime() : (DateTime?)null;

                        var assignmentName = "Untitled Assignment";
                        var assignmentId = "unknown";

                        if (s.TryGetProperty("assignment", out var a))
                        {
                            assignmentName = a.TryGetProperty("name", out var an) ? an.GetString() ?? "Untitled" : "Untitled";
                            assignmentId = a.TryGetProperty("id", out var aid) ? aid.ToString() : "unknown";
                        }
                        else if (s.TryGetProperty("assignment_id", out var aid2))
                        {
                            assignmentId = aid2.ToString();
                        }

                        list.Add(new CanvasGrade
                        {
                            AssignmentId = assignmentId,
                            AssignmentName = assignmentName,
                            Grade = grade,
                            Score = score,
                            GradedAt = gradedAt
                        });
                    }

                    _logger.LogInformation("Fetched {Count} Canvas grades for course {CourseId}", list.Count, courseId);
                    return list;
                }
                else
                {
                    _logger.LogWarning("Failed to fetch grades: {StatusCode}", response.StatusCode);
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
                // To get announcements, we typically need context codes (course IDs).
                // First, fetch active courses.
                var coursesUrl = $"https://{canvasDomain}/api/v1/courses?enrollment_state=active&per_page=10";
                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");

                var coursesResponse = await _httpClient.GetAsync(coursesUrl);
                var contextCodes = new List<string>();

                if (coursesResponse.IsSuccessStatusCode)
                {
                    var coursesJson = await coursesResponse.Content.ReadAsStringAsync();
                    var courses = JsonDocument.Parse(coursesJson).RootElement;
                    foreach (var c in courses.EnumerateArray())
                    {
                        if (c.TryGetProperty("id", out var cid))
                        {
                            contextCodes.Add($"course_{cid}");
                        }
                    }
                }

                if (contextCodes.Count == 0) return new List<CanvasAnnouncement>();

                // Now fetch announcements for these contexts
                // context_codes[]=course_123&context_codes[]=course_456
                var queryParams = string.Join("&", contextCodes.Select(c => $"context_codes[]={c}"));
                var announcementsUrl = $"https://{canvasDomain}/api/v1/announcements?{queryParams}&per_page=10";

                var response = await _httpClient.GetAsync(announcementsUrl);
                if (response.IsSuccessStatusCode)
                {
                     var json = await response.Content.ReadAsStringAsync();
                     var items = JsonDocument.Parse(json).RootElement;
                     var list = new List<CanvasAnnouncement>();

                     foreach (var a in items.EnumerateArray())
                     {
                         var id = a.TryGetProperty("id", out var aid) ? aid.ToString() : Guid.NewGuid().ToString();
                         var title = a.TryGetProperty("title", out var t) ? t.GetString() ?? "No Title" : "No Title";
                         var message = a.TryGetProperty("message", out var m) ? m.GetString() ?? "" : ""; // HTML content
                         var postedAt = a.TryGetProperty("posted_at", out var pa) && pa.ValueKind == JsonValueKind.String
                             && DateTime.TryParse(pa.GetString(), out var dt) ? dt.ToUniversalTime() : DateTime.UtcNow;
                         var courseId = a.TryGetProperty("context_code", out var cc) ? cc.GetString() ?? "" : "";

                         list.Add(new CanvasAnnouncement
                         {
                             Id = id,
                             Title = title,
                             Message = message, // Note: This is usually HTML
                             PostedAt = postedAt,
                             CourseId = courseId
                         });
                     }

                    _logger.LogInformation("Fetched {Count} Canvas announcements", list.Count);
                    return list;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching Canvas announcements");
            }

            return new List<CanvasAnnouncement>();
        }
    }
}
