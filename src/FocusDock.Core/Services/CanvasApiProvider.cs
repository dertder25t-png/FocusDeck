namespace FocusDock.Core.Services;

using System.Net.Http;
using System.Text.Json;
using FocusDock.Data.Models;

/// <summary>
/// Canvas LMS API integration
/// </summary>
public class CanvasApiProvider
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;
    private readonly string _apiToken;

    public CanvasApiProvider(string baseUrl, string apiToken)
    {
        _baseUrl = baseUrl.TrimEnd('/');
        _apiToken = apiToken;
        _httpClient = new HttpClient();
    }

    /// <summary>
    /// Test the Canvas connection
    /// </summary>
    public async Task<bool> TestConnection()
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"{_baseUrl}/api/v1/users/self");
            request.Headers.Add("Authorization", $"Bearer {_apiToken}");

            var response = await _httpClient.SendAsync(request);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Get enrolled courses for the current user
    /// </summary>
    public async Task<List<CanvasAssignment>> FetchAssignments()
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get,
                $"{_baseUrl}/api/v1/courses?enrollment_state=active&include=total_scores");
            request.Headers.Add("Authorization", $"Bearer {_apiToken}");

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode) return new();

            var content = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(content);

            var allAssignments = new List<CanvasAssignment>();

            foreach (var course in doc.RootElement.EnumerateArray())
            {
                var courseId = course.GetProperty("id").GetInt32();
                var courseName = course.GetProperty("name").GetString() ?? "";

                // Fetch assignments for this course
                var assignmentsRequest = new HttpRequestMessage(HttpMethod.Get,
                    $"{_baseUrl}/api/v1/courses/{courseId}/assignments?include=submission");
                assignmentsRequest.Headers.Add("Authorization", $"Bearer {_apiToken}");

                var assignmentsResponse = await _httpClient.SendAsync(assignmentsRequest);
                if (!assignmentsResponse.IsSuccessStatusCode) continue;

                var assignmentsContent = await assignmentsResponse.Content.ReadAsStringAsync();
                using var assignmentDoc = JsonDocument.Parse(assignmentsContent);

                foreach (var assignment in assignmentDoc.RootElement.EnumerateArray())
                {
                    var dueAtStr = assignment.TryGetProperty("due_at", out var dueAt)
                        ? dueAt.GetString()
                        : null;

                    var dueDate = DateTime.MaxValue;
                    if (!string.IsNullOrEmpty(dueAtStr))
                    {
                        if (DateTime.TryParse(dueAtStr, out var parsed))
                            dueDate = parsed;
                    }

                    var canvasAssignment = new CanvasAssignment
                    {
                        Id = assignment.GetProperty("id").GetInt32().ToString(),
                        Title = assignment.GetProperty("name").GetString() ?? "",
                        CourseName = courseName,
                        DueDate = dueDate,
                        PointsPossible = assignment.TryGetProperty("points_possible", out var points)
                            ? points.GetDouble()
                            : 0,
                        SubmittedAt = assignment.TryGetProperty("submission", out var submission) &&
                                     submission.TryGetProperty("submitted_at", out var submitted) &&
                                     submitted.ValueKind != JsonValueKind.Null
                            ? DateTime.Parse(submitted.GetString() ?? DateTime.Now.ToString("o"))
                            : null,
                        SubmissionUrl = $"{_baseUrl}/courses/{courseId}/assignments/{assignment.GetProperty("id").GetInt32()}"
                    };

                    allAssignments.Add(canvasAssignment);
                }
            }

            return allAssignments;
        }
        catch
        {
            return new();
        }
    }

    /// <summary>
    /// Get specific course assignments
    /// </summary>
    public async Task<List<CanvasAssignment>> FetchCourseAssignments(int courseId, string courseName)
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get,
                $"{_baseUrl}/api/v1/courses/{courseId}/assignments?include=submission");
            request.Headers.Add("Authorization", $"Bearer {_apiToken}");

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode) return new();

            var content = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(content);

            var assignments = new List<CanvasAssignment>();

            foreach (var assignment in doc.RootElement.EnumerateArray())
            {
                var dueAtStr = assignment.TryGetProperty("due_at", out var dueAt)
                    ? dueAt.GetString()
                    : null;

                var dueDate = DateTime.MaxValue;
                if (!string.IsNullOrEmpty(dueAtStr))
                {
                    if (DateTime.TryParse(dueAtStr, out var parsed))
                        dueDate = parsed;
                }

                var canvasAssignment = new CanvasAssignment
                {
                    Id = assignment.GetProperty("id").GetInt32().ToString(),
                    Title = assignment.GetProperty("name").GetString() ?? "",
                    CourseName = courseName,
                    DueDate = dueDate,
                    PointsPossible = assignment.TryGetProperty("points_possible", out var points)
                        ? points.GetDouble()
                        : 0,
                    SubmittedAt = assignment.TryGetProperty("submission", out var submission) &&
                                 submission.TryGetProperty("submitted_at", out var submitted) &&
                                 submitted.ValueKind != JsonValueKind.Null
                        ? DateTime.Parse(submitted.GetString() ?? DateTime.Now.ToString("o"))
                        : null,
                    SubmissionUrl = $"{_baseUrl}/courses/{courseId}/assignments/{assignment.GetProperty("id").GetInt32()}"
                };

                assignments.Add(canvasAssignment);
            }

            return assignments;
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
